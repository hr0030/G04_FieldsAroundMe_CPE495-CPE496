// FAM Embedded Code
// Writers: Jacob Dodd & Hayden Rose
// Mentor: Dr. Emil Jovanov
// CPE Senior Design F24-S25

/******************************************
*                INCLUDES               *
******************************************/
#include <stdio.h>
#include <time.h>
#include <sys/time.h>
#include <string.h>
#include "esp_err.h"
#include "esp_log.h"
#include "esp_timer.h"
#include "freertos/portmacro.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "driver/spi_common.h"
#include "driver/spi_master.h"
#include "hal/spi_types.h"
#include "sdkconfig.h"
#include "AD_Defs.h"
#include "esp_mac.h"
#include "esp_wifi.h"
#include "nvs_flash.h"
#include "mqtt_client.h"
#include "esp_netif.h"
#include "esp_event.h"
#include "driver/uart.h"
#include "driver/gpio.h"
#include "esp_sntp.h"
#include "freertos/semphr.h"
#include <inttypes.h>

/******************************************
*          DEFINITIONS & MACROS         *
******************************************/
//#define ENABLE_MULTI_CHANNELS  // Comment out to disable multi-channel functionality
#define TAG "ESP32_Project"
static SemaphoreHandle_t adc_semaphore;  // Semaphore to signal ADC read
esp_timer_handle_t adc_timer;            // Timer handle
static esp_timer_handle_t adc_timer_handle;

// ENTER YOUR WIFI INFO HERE
#define ESP_WIFI_SSID      "Student5"
#define ESP_WIFI_PASS      "Go Chargers!"

// Sampling Frequency
uint16_t fs = 48;   // FS values of 19 and 48 correspond to ~250Hz and ~100Hz per channel, respectively

// UART Configurations for OpenLog. 
#define UART_PORT_NUM      UART_NUM_1
#define UART_BAUD_RATE     9600
#define UART_TX_PIN        GPIO_NUM_16
#define UART_RX_PIN        GPIO_NUM_17
#define OPENLOG_RESET_PIN  GPIO_NUM_10
#define BUF_SIZE           1024
 
spi_device_handle_t handle;

// MQTT variables
static esp_mqtt_client_handle_t client;
static bool mqtt_connected = false;
char Current_Date_Time[100];
uint8_t txData[4] = {0};

/****************************
*      UART FUNCTIONS     *
****************************/
void openlog_uart_init() 
{
    // UART configuration
    const uart_config_t uart_config = 
    {
        .baud_rate = UART_BAUD_RATE,
        .data_bits = UART_DATA_8_BITS,
        .parity = UART_PARITY_DISABLE,
        .stop_bits = UART_STOP_BITS_1,
        .flow_ctrl = UART_HW_FLOWCTRL_DISABLE,
    };

    // Install UART driver and set pins
    uart_driver_install(UART_PORT_NUM, BUF_SIZE * 2, 0, 0, NULL, 0);
    uart_param_config(UART_PORT_NUM, &uart_config);
    uart_set_pin(UART_PORT_NUM, UART_TX_PIN, UART_RX_PIN, UART_PIN_NO_CHANGE, UART_PIN_NO_CHANGE);

    printf("UART Initialized!\n");
}

void reset_openlog() 
{
    gpio_config_t io_conf = 
    {
        .pin_bit_mask = (1ULL << OPENLOG_RESET_PIN),
        .mode = GPIO_MODE_OUTPUT,
        .pull_up_en = GPIO_PULLUP_DISABLE,
        .pull_down_en = GPIO_PULLDOWN_DISABLE,
        .intr_type = GPIO_INTR_DISABLE
    };
    gpio_config(&io_conf);

    // Send a reset pulse
    printf("Resetting Openlog...\n");
    gpio_set_level(OPENLOG_RESET_PIN, 0); // Assert reset (low)
    vTaskDelay(pdMS_TO_TICKS(100));       // Hold for 100 ms
    gpio_set_level(OPENLOG_RESET_PIN, 1); // Deassert reset (high)
    vTaskDelay(pdMS_TO_TICKS(100));       // Wait for OpenLog to initialize
    printf("Reset Complete!\n");
}

void log_to_openlog(const char *log_line) 
{
    // Send the log line to UART
    uart_write_bytes(UART_PORT_NUM, log_line, strlen(log_line));
    //printf("Openlog Recieved: %s", log_line);
}

/******************************************
*          SPI CONFIG FUNCTIONS         *
******************************************/
static void spi_init()
{
    spi_bus_config_t buscfg = 
    {
        
        .miso_io_num = 20, // 22, // 12,
        .mosi_io_num = 19, // 21, // 13,
        .sclk_io_num = 21, // 17, // 14,
        .quadwp_io_num = -1,
        .quadhd_io_num = -1,
        .max_transfer_sz = 32,
        .flags = 0,
    };
 
    ESP_ERROR_CHECK(spi_bus_initialize(SPI2_HOST, &buscfg, SPI_DMA_DISABLED)); // SPI_DMA_CH_AUTO
 
    spi_device_interface_config_t devcfg = 
    {
        .address_bits = 0,//8, // set if using .addr in transaction
        .command_bits = 0, // set if using .cmd in transaction
        .dummy_bits = 0,
        .mode = 0,
        .duty_cycle_pos = 0,
        .cs_ena_posttrans = 0,
        .cs_ena_pretrans = 0,
        .clock_speed_hz = 614400, // 614.4kHz SPI CLOCK SPEED
        .spics_io_num = 18,
        .flags = 0, 
        .queue_size = 2,
        .pre_cb = NULL,
        .post_cb = NULL,
    };
    ESP_ERROR_CHECK(spi_bus_add_device(SPI2_HOST, &devcfg, &handle));
};
 
int spiRead(uint8_t *txBuffer, uint8_t *rxBuffer, size_t len)
{
    spi_transaction_t trans_desc = 
    {
        .length = 8 * len,             // Total transaction length in bits
        .tx_buffer = txBuffer,        // Data to send
        .rx_buffer = rxBuffer,        // Buffer to receive data
    };

    esp_err_t ret = spi_device_transmit(handle, &trans_desc);
    if (ret != ESP_OK) 
    {
        printf("SPI Read Error: %d\n", ret);
    }

    return ret;
}

int spiWrite(uint8_t *txBuffer, size_t len)
{
    spi_transaction_t trans_desc = 
    {
        .length = 8 * len,             // Total transaction length in bits
        .tx_buffer = txBuffer,        // Data to send
        .rx_buffer = NULL,            
    };

    esp_err_t ret = spi_device_transmit(handle, &trans_desc);
    if (ret != ESP_OK) 
    {
        printf("SPI Write Error: %d\n", ret);
    }

    return ret;
}

/******************************************
*           ADC CONTROL FUNCTIONS       *
******************************************/
void ADC_reset()
{
    uint8_t wr_buf[8] = {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};
    spiWrite(wr_buf, sizeof(wr_buf)); // Pass the length explicitly
}

int adcWrite(uint8_t reg, uint8_t *data, size_t len)
{
    uint8_t txBuffer[len + 1];
    txBuffer[0] = AD7124_COMM_REG_WR | AD7124_COMM_REG_RA(reg);
    memcpy(&txBuffer[1], data, len);

    return spiWrite(txBuffer, len + 1);
}

int adcRead(uint8_t reg, uint8_t *data, size_t len)
{
    uint8_t txBuffer[1] = {AD7124_COMM_REG_RD | AD7124_COMM_REG_RA(reg)};
    uint8_t rxBuffer[len + 1]; // Response: 1 command echo + actual data

    spi_transaction_t trans_desc = 
    {
        .length = 8 * (len + 1),   // Total length in bits
        .tx_buffer = txBuffer,     // Send only the command byte
        .rx_buffer = rxBuffer,     // Read back len + 1 bytes
        .rxlength = 8 * (len + 1)  // Length of response to read
    };

    esp_err_t ret = spi_device_transmit(handle, &trans_desc);
    if (ret != ESP_OK) 
    {
        printf("SPI Read Error: %d\n", ret);
        return ret;
    }

    memcpy(data, &rxBuffer[1], len);    // Copy only the actual data (ignore first byte)

    printf("ADC Read [0x%02X]:", reg);
    for (size_t i = 0; i < len; i++) 
    {
        printf(" 0x%02X", data[i]);
    }
    printf("\n");

    return 0;
}

int readData(int32_t *pData)
{
    uint8_t txBuffer[1] = {AD7124_COMM_REG_RD | AD7124_COMM_REG_RA(AD7124_DATA_REG)};
    uint8_t rxBuffer[4] = {0}; // Ensure space for potential leading dummy byte

    // Perform SPI read transaction
    int ret = spiRead(txBuffer, rxBuffer, sizeof(rxBuffer));
    if (ret != ESP_OK) 
    {
        printf("SPI Read Error\n");
        return ret;
    }

    /*// Debug: Print raw ADC bytes received
    uint32_t rawHexValue = ((uint32_t)rxBuffer[1] << 16) | ((uint32_t)rxBuffer[2] << 8) | rxBuffer[3];
    printf("Raw ADC Bytes: 0x%02X 0x%02X 0x%02X |", rxBuffer[1], rxBuffer[2], rxBuffer[3]);*/

    *pData = (rxBuffer[1] << 16) | (rxBuffer[2] << 8) | rxBuffer[3];    // Combine the 3 bytes into a 24-bit signed value
    return 0;
}

long getData()
{
    int32_t value = 0;
    int ret = readData(&value);

    if (ret != 0) 
    {
        return ret; // Return error if the read fails
    }

    return (long)value;
}

void IRAM_ATTR adc_timer_callback(void* arg) // interrupt for sampling
{
    BaseType_t xHigherPriorityTaskWoken = pdFALSE;
    xSemaphoreGiveFromISR(adc_semaphore, &xHigherPriorityTaskWoken);
    portYIELD_FROM_ISR(xHigherPriorityTaskWoken);
}

void setup_adc_timer(uint64_t interval_us) 
{
    esp_timer_create_args_t adc_timer_args = {
        .callback = &adc_timer_callback,
        .arg = NULL,
        .name = "adc_timer"
    };

    ESP_ERROR_CHECK(esp_timer_create(&adc_timer_args, &adc_timer_handle));
    ESP_ERROR_CHECK(esp_timer_start_periodic(adc_timer_handle, interval_us));
    printf("ADC Timer set to %llu us interval\n", interval_us);
}

double toVoltage(long value, int gain, double vref, bool bipolar) 
{
    double voltage = (double)value;
    if (bipolar) 
    {
        // Bipolar mode: normalize ADC value to [-REF, REF]
        voltage = voltage / (double)0x7FFFFF - 1.0;
    } 
    else 
    {
        // Unipolar mode: normalize ADC value to [0, REF]
        voltage = voltage / (double)0xFFFFFF;
    }

    // Apply reference voltage and gain scaling
    voltage = voltage * vref / (double)gain;
    return voltage;
}

int setAdcControl(uint8_t mode, uint8_t power, uint8_t clkSource, bool enable)
{
    uint16_t control = 0;
    control |= AD7124_ADC_CTRL_REG_MODE(mode);  // Set mode (bits 5:2)
    control |= AD7124_ADC_CTRL_REG_POWER_MODE(power);   // Set power mode (bits 7:6)
    control |= AD7124_ADC_CTRL_REG_CLK_SEL(clkSource);  // Set clock source (bits 1:0)
    
    if (enable) 
    {
        control |= AD7124_ADC_CTRL_REG_REF_EN;  // Enable internal reference 
    }
    
    control |= AD7124_ADC_CTRL_REG_CONT_READ; 
    control |= AD7124_ADC_CTRL_REG_CS_EN;

    uint8_t controlBytes[2] = { (uint8_t)(control >> 8), (uint8_t)(control & 0xFF) };
    return adcWrite(AD7124_ADC_CTRL_REG, controlBytes, 2);
}

int setConfig(uint8_t configNum, uint8_t reference, uint8_t gain, bool bipolar)
{
    uint16_t config = 0;

    config |= AD7124_CFG_REG_REF_SEL(reference);
    config |= AD7124_CFG_REG_PGA(gain);
    if (bipolar) 
    {
        config |= AD7124_CFG_REG_BIPOLAR;
    }

    uint8_t configBytes[2] = {config >> 8, config & 0xFF};
    return adcWrite(AD7124_CFG0_REG + configNum, configBytes, 2);
}

int setConfigFilter(uint8_t filterNum, uint8_t filterType, uint16_t fs, bool enableRej60) 
{
    uint32_t filter = 0;
    uint16_t fs_reg = (uint16_t)fs;
    
    filter |= AD7124_FILT_REG_FILTER(filterType);
    if (enableRej60) filter |= AD7124_FILT_REG_REJ60;
    filter |= AD7124_FILT_REG_FS(fs_reg);
    
    // Create filter configuration bytes
    uint8_t filterBytes[3] = {
        (filter >> 16) & 0xFF,
        (filter >> 8) & 0xFF,
        filter & 0xFF
    };
    
    // Write the filter configuration
    int status = adcWrite(AD7124_FILT0_REG + filterNum, filterBytes, 3);
    return status;
}

int setChannel(uint8_t channelNum, uint8_t configNum, uint8_t posInput, uint8_t negInput, bool enable)
{
    uint16_t channel = 0;

    channel |= AD7124_CH_MAP_REG_SETUP(configNum);
    channel |= AD7124_CH_MAP_REG_AINP(posInput);
    channel |= AD7124_CH_MAP_REG_AINM(negInput);
    if (enable) 
    {
        channel |= AD7124_CH_MAP_REG_CH_ENABLE;
    }

    uint8_t channelBytes[2] = {channel >> 8, channel & 0xFF};
    return adcWrite(AD7124_CH0_MAP_REG + channelNum, channelBytes, 2);
}

int enableChannel(uint8_t channelNum, bool enable)
{
    uint8_t channelBytes[2] = {0};
    int status = adcRead(AD7124_CH0_MAP_REG + channelNum, channelBytes, 2);
    if (status != 0) 
    {
        return status;
    }

    uint16_t channel = (channelBytes[0] << 8) | channelBytes[1];
    if (enable) 
    {
        channel |= AD7124_CH_MAP_REG_CH_ENABLE; // Enable the channel
    } 
    else 
    {
        channel &= ~AD7124_CH_MAP_REG_CH_ENABLE; // Disable the channel
    }

    channelBytes[0] = (channel >> 8) & 0xFF;
    channelBytes[1] = channel & 0xFF;

    return adcWrite(AD7124_CH0_MAP_REG + channelNum, channelBytes, 2);
}

void ad7124_init(uint16_t fs) 
{
    int status = setAdcControl(0x00, 0x02, 0x00, false); // Continuous conversion/read mode, Full-power, internal clk, internal ref = false
    printf("ADC control set status: %d\n", status);

    status = setConfig(0, 0x00, 0x00, true);    // config #1, REFIN+ & REFIN- references, gain = 1, bipolar mode on
    printf("ADC configuration set status: %d\n", status);

    status = setConfigFilter(0, 0x04, fs, true); // Sinc4 Filter, variable FS, 60Hz notch filter enabled
    printf("ADC filter set status: %d\n", status);

    /*double fADC = 614400.0 / (32.0 * fs);
    double sampling_interval_ms = (1.0 / fADC) * 1000.0;    //DEBUG STUFF FOR FINDING SAMPLING FREQUENCY
    printf("ADC Configured: FS=%d, fADC=%.2f SPS, Sampling Interval=%.3f ms\n", fs, fADC, sampling_interval_ms);*/

    // Enable all 4 ADC channels (Differential Mode)
    status = setChannel(0, 0, 0x00, 0x01, true); // AIN0 (+) to AIN1 (-)
    printf("Channel 0 set status: %d\n", status);
    status = setChannel(1, 0, 0x02, 0x03, true); // AIN2 (+) to AIN3 (-)
    printf("Channel 1 set status: %d\n", status);
    status = setChannel(2, 0, 0x04, 0x05, true); // AIN4 (+) to AIN5 (-)
    printf("Channel 2 set status: %d\n", status);
    status = setChannel(3, 0, 0x06, 0x07, true); // AIN6 (+) to AIN7 (-)
    printf("Channel 3 set status: %d\n", status);

    for (int i = 0; i < 4; i++) 
    {
        status = enableChannel(i, true);    // enable all channels
        printf("Channel %d enable status: %d\n", i, status);
    }
}

/******************************************
*            WIFI FUNCTIONS             *
******************************************/
// Wi-Fi Event Handler
static void wifi_event_handler(void *arg, esp_event_base_t event_base, int32_t event_id, void *event_data) 
{
    if (event_base == WIFI_EVENT && event_id == WIFI_EVENT_STA_START) 
    {
        esp_wifi_connect();
    } 
    else if (event_base == WIFI_EVENT && event_id == WIFI_EVENT_STA_DISCONNECTED) 
    {
        printf("Wi-Fi disconnected, reconnecting...");
        esp_wifi_connect();
    } 
    else if (event_base == IP_EVENT && event_id == IP_EVENT_STA_GOT_IP) 
    {
        ip_event_got_ip_t *event = (ip_event_got_ip_t *)event_data;
        ESP_LOGI(TAG, "Got IP Address: " IPSTR, IP2STR(&event->ip_info.ip));
    }
}

// Initialize Wi-Fi
static void wifi_init(void) 
{
    esp_netif_init();
    esp_event_loop_create_default();
    esp_netif_create_default_wifi_sta();

    wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();
    esp_wifi_init(&cfg);

    esp_event_handler_register(WIFI_EVENT, ESP_EVENT_ANY_ID, &wifi_event_handler, NULL);
    esp_event_handler_register(IP_EVENT, IP_EVENT_STA_GOT_IP, &wifi_event_handler, NULL);

    wifi_config_t wifi_config = 
    {
        .sta = 
        {
            .ssid = ESP_WIFI_SSID,
            .password = ESP_WIFI_PASS
        },
    };
    esp_wifi_set_mode(WIFI_MODE_STA);
    esp_wifi_set_config(WIFI_IF_STA, &wifi_config);
    esp_wifi_start();
}

/******************************************
*           MQTT & SNTP FUNCTIONS         *
******************************************/
// MQTT Event Handler
static void mqtt_event_handler(void *handler_args, esp_event_base_t base, int32_t event_id, void *event_data) 
{
    esp_mqtt_event_handle_t event = (esp_mqtt_event_handle_t)event_data;

    switch ((esp_mqtt_event_id_t)event_id) 
    {
    case MQTT_EVENT_CONNECTED:
        printf("\nMQTT Connected\n");
        mqtt_connected = true;
        break;

    case MQTT_EVENT_DISCONNECTED:
        printf("\nMQTT Disconnected\n");
        mqtt_connected = false;
        break;

    case MQTT_EVENT_PUBLISHED:
        printf("\nMessage ID Sent: %d\n", event->msg_id);
        break;

    case MQTT_EVENT_ERROR:
        printf("\nMQTT Error\n");
        break;

    default:
        ESP_LOGI(TAG, "\nUnhandled MQTT Event: %" PRId32, event_id);
        break;
    }
}

// MQTT Initialization
void mqtt_app_start(void) 
{
    esp_mqtt_client_config_t mqtt_cfg = 
    {
        .broker.address.uri = "mqtt://10.4.175.17", // ENTER THE IP OF THE MACHINE RUNNING THE SERVER HERE
        .broker.address.port = 1883
    };

    client = esp_mqtt_client_init(&mqtt_cfg);
    esp_mqtt_client_register_event(client, ESP_EVENT_ANY_ID, mqtt_event_handler, client);
    esp_mqtt_client_start(client);
}

static void init_SNTP(void)
{
    //ESP_LOGI(TAG, "Initializing SNTP");
    printf("Initializing SNTP\n");
    esp_sntp_setoperatingmode(SNTP_OPMODE_POLL);
    esp_sntp_setservername(0, "pool.ntp.org");  // calls on external server to grab real time information
    esp_sntp_set_time_sync_notification_cb(NULL);  // No callback needed for now
    esp_sntp_init();
}

static void obtain_time(void)
{
    init_SNTP();
    time_t now = 0;
    struct tm timeinfo = { 0 };
    int retry = 0;
    const int retry_count = 10;

    while (sntp_get_sync_status() == SNTP_SYNC_STATUS_RESET && ++retry < retry_count) 
    {
        //ESP_LOGI(TAG, "Waiting for system time to sync... (%d/%d)", retry, retry_count);
        printf("Waiting for Sync... (%d|%d)\n", retry, retry_count);
        vTaskDelay(2000 / portTICK_PERIOD_MS);
    }
    time(&now);
    localtime_r(&now, &timeinfo);
}

void Get_current_date_time(char *date_time)
{
    time_t now;
    struct tm timeinfo;
    time(&now);
    localtime_r(&now, &timeinfo);

    // Set timezone 
    setenv("TZ", "UTC+05:00", 1); // UTC-5 (CST)
    tzset();
    strftime(date_time, 100, "%Y-%m-%d %H:%M:%S", &timeinfo);
}

void Set_SystemTime_SNTP(void)
{
    time_t now;
    struct tm timeinfo;
    time(&now);
    localtime_r(&now, &timeinfo);

    // Check if time is set 
    if (timeinfo.tm_year < (2016 - 1900)) 
    {
        //ESP_LOGI(TAG, "Time is not set. Connecting to WiFi and syncing via NTP.");
        printf("Time not set! Attempting to Re-Sync...\n");
        obtain_time();
        time(&now); // Update time after syncing
    }
}

/******************************************
*           MAIN APPLICATION            *
******************************************/
void app_main(void) 
{
    // Initialize NVS for Wi-Fi and MQTT
    esp_err_t ret = nvs_flash_init();
    if (ret == ESP_ERR_NVS_NO_FREE_PAGES || ret == ESP_ERR_NVS_NEW_VERSION_FOUND) 
    {
        ESP_ERROR_CHECK(nvs_flash_erase());
        ret = nvs_flash_init();
    }
    ESP_ERROR_CHECK(ret);

    wifi_init();    // Initialize Wi-Fi 
    vTaskDelay(2000 / portTICK_PERIOD_MS); // Ensure Wi-Fi stabilizes before SNTP
    Set_SystemTime_SNTP();  // Initialize and set SNTP
    vTaskDelay(50 / portTICK_PERIOD_MS);
    mqtt_app_start();   // Start MQTT 
    vTaskDelay(50 / portTICK_PERIOD_MS);
    spi_init();     // SPI initialization for ADC
    vTaskDelay(50 / portTICK_PERIOD_MS);
    ADC_reset();    // Reset the ADC
    vTaskDelay(50 / portTICK_PERIOD_MS);
    ad7124_init(fs);    // ADC initialization. Filter Setting (FS) divisor value determined at top of program
    vTaskDelay(50 / portTICK_PERIOD_MS);
    reset_openlog();    // Reset Openlog
    openlog_uart_init();    // Initialize Openlog

    const double f_master = 614400.0; // Full power mode master clock (Hz)
    double fADC = f_master / (32.0 * fs);   // Total ADC sampling rate
    int enabled_channels = 4;  // Set to the number of active ADC channels
    double per_channel_sps = fADC / enabled_channels; // Per-channel sampling rate
    uint64_t sample_period_us = (uint64_t)(1000000.0 / per_channel_sps); // Microsecond period

    adc_semaphore = xSemaphoreCreateBinary(); // Create semaphore
    setup_adc_timer(sample_period_us);  // Set up timer
    uint64_t last_time = esp_timer_get_time(); // Initial timestamp

    while (1) 
    {
        if (xSemaphoreTake(adc_semaphore, portMAX_DELAY) == 1) 
        {
            int32_t adcValue = 0;
            if (readData(&adcValue) == 0) 
            {
                /*double voltage = toVoltage(adcValue, 1, 3.0, true);
                char log_line[150];
                Get_current_date_time(Current_Date_Time);
                snprintf(log_line, sizeof(log_line), "%s, Channel, %g V\n", Current_Date_Time, voltage);

                if (mqtt_connected) 
                {
                    char message[50];
                    snprintf(message, sizeof(message), "Channel 0: %g V", voltage);
                    esp_mqtt_client_publish(client, "esp32/sensor/data", message, 0, 1, 0);
                } 
                log_to_openlog(log_line);*/   // **Log to SD card**
                uint64_t current_time = esp_timer_get_time();
                double actual_sps = 1.0 / ((current_time - last_time) / 1000000.0);
                last_time = current_time;
                printf("%.1f Hz\n", actual_sps);
            } 
            else 
            {
                printf("Error reading ADC data\n");
            }
        }
    }
}