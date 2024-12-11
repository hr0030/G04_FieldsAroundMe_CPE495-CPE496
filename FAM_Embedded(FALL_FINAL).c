// Prototype Embedded Code
// Jacob Dodd & Hayden Rose

#include <stdio.h>
#include <time.h>
#include <string.h>
#include "esp_err.h"
#include "esp_log.h"
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
#include <inttypes.h>


#define TAG "ESP32_Project"

// UART Configurations for OpenLog
#define UART_PORT_NUM      UART_NUM_1
#define UART_BAUD_RATE     9600
#define UART_TX_PIN        GPIO_NUM_16
#define UART_RX_PIN        GPIO_NUM_17
#define BUF_SIZE           1024
#define OPENLOG_RESET_PIN  GPIO_NUM_10
 
spi_device_handle_t handle;
 
uint8_t txData[4] = {0};
//uint8_t rxData[4] = {0};

// Reset OpenLog before data logging
void reset_openlog() {
    // Configure reset pin as output
    gpio_config_t io_conf = {
        .pin_bit_mask = (1ULL << OPENLOG_RESET_PIN),
        .mode = GPIO_MODE_OUTPUT,
        .pull_up_en = GPIO_PULLUP_DISABLE,
        .pull_down_en = GPIO_PULLDOWN_DISABLE,
        .intr_type = GPIO_INTR_DISABLE
    };
    gpio_config(&io_conf);

    // Send a reset pulse
    ESP_LOGI(TAG, "Resetting OpenLog...");
    gpio_set_level(OPENLOG_RESET_PIN, 0); // Assert reset (low)
    vTaskDelay(pdMS_TO_TICKS(100));       // Hold for 100 ms
    gpio_set_level(OPENLOG_RESET_PIN, 1); // Deassert reset (high)
    vTaskDelay(pdMS_TO_TICKS(100));       // Wait for OpenLog to initialize
    ESP_LOGI(TAG, "OpenLog reset complete.");
}

// Initialize UART for OpenLog communication
void openlog_uart_init() {
    // UART configuration
    const uart_config_t uart_config = {
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

    ESP_LOGI(TAG, "OpenLog UART initialized.");
}

// Log ADC data to OpenLog
void log_to_openlog(const char *log_line) {
    // Send the log line to UART
    uart_write_bytes(UART_PORT_NUM, log_line, strlen(log_line));
    ESP_LOGI(TAG, "Logged to OpenLog: %s", log_line);
}

 
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
        .mode = 3,
        .duty_cycle_pos = 0,
        .cs_ena_posttrans = 0,
        .cs_ena_pretrans = 0,
        .clock_speed_hz = 19200,
        .spics_io_num = 18,
        .flags = 0, // SPI_DEVICE_HALFDUPLEX,
        .queue_size = 1,
        .pre_cb = NULL,
        .post_cb = NULL,
    };
    ESP_ERROR_CHECK(spi_bus_add_device(SPI2_HOST, &devcfg, &handle));
};
 
int spiRead(uint8_t *txBuffer, uint8_t *rxBuffer, size_t len)
{
    spi_transaction_t trans_desc = {
        .length = 8 * len,             // Total transaction length in bits
        .tx_buffer = txBuffer,        // Data to send
        .rx_buffer = rxBuffer,        // Buffer to receive data
    };

    esp_err_t ret = spi_device_transmit(handle, &trans_desc);
    if (ret != ESP_OK) {
        printf("SPI Read Error: %d\n", ret);
    }

    return ret;
}

int spiWrite(uint8_t *txBuffer, size_t len)
{
    spi_transaction_t trans_desc = {
        .length = 8 * len,             // Total transaction length in bits
        .tx_buffer = txBuffer,        // Data to send
        .rx_buffer = NULL,            
    };

    esp_err_t ret = spi_device_transmit(handle, &trans_desc);
    if (ret != ESP_OK) {
        printf("SPI Write Error: %d\n", ret);
    }

    return ret;
}
 
void reset()
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
    uint8_t txBuffer[len + 1]; // Command byte + dummy bytes
    uint8_t rxBuffer[len + 1]; // Command echo + response bytes

    // Construct the read command
    txBuffer[0] = AD7124_COMM_REG_RD | AD7124_COMM_REG_RA(reg);
    memset(&txBuffer[1], 0x00, len); // Dummy bytes for clocking out response

    // Perform SPI transaction
    int ret = spiRead(txBuffer, rxBuffer, len + 1);
    if (ret != ESP_OK) {
        return ret;
    }

    // Copy received data, ignoring the first byte (command echo)
    memcpy(data, &rxBuffer[1], len);

    printf("ADC Read [0x%02X]:", reg);
    for (size_t i = 0; i < len; i++) {
        printf(" 0x%02X", data[i]);
    }
    printf("\n");

    return ESP_OK;
}

int readRegister(uint8_t reg, uint8_t *value, size_t len)
{
    return adcRead(reg, value, len);
}

int readData(int32_t *pData)
{
    uint8_t txBuffer[1] = {AD7124_COMM_REG_RD | AD7124_COMM_REG_RA(AD7124_DATA_REG)};
    uint8_t rxBuffer[3]; 

    int ret = spiRead(txBuffer, rxBuffer, sizeof(rxBuffer) + 1);
    if (ret != ESP_OK) 
    {
        return ret;
    }

    // Debug: Print raw ADC bytes received
    //printf("Raw ADC Bytes: 0x%02X 0x%02X 0x%02X\n", rxBuffer[0], rxBuffer[1], rxBuffer[2]);

    *pData = (rxBuffer[0] << 16) | (rxBuffer[1] << 8) | rxBuffer[2];

    return ESP_OK;
}

long getData()
{
    int32_t value = 0;
    int ret = readData(&value);

    if (ret != ESP_OK) 
    {
        return ret; // Return error if the read fails
    }

    return (long)value;
}

double toVoltage(long value, int gain, double vref, bool bipolar)
{
    double voltage = (double)value;

    if (bipolar) 
    {
        // Bipolar mode(SPRING): scale and shift the ADC value
        voltage = voltage / (double)0x7FFF - 1.0;
    } 
    else 
    {
        // Unipolar mode(FALL): scale the ADC value 16-bits for right now
        voltage = voltage / (double)0xFFFF;
    }

    // Apply reference voltage and gain scaling
    voltage = voltage * vref / (double)gain;

    return voltage;
}

int setAdcControl(uint8_t mode, uint8_t power, uint8_t clkSource, bool enable)
{
    uint16_t control = 0;
    uint16_t test = 1;

    control |= AD7124_ADC_CTRL_REG_MODE(mode);
    control |= AD7124_ADC_CTRL_REG_POWER_MODE(power);
    control |= AD7124_ADC_CTRL_REG_CLK_SEL(clkSource);
    if (enable) 
    {
        control |= AD7124_ADC_CTRL_REG_REF_EN;
    }
    control |= AD7124_ADC_CTRL_REG_CS_EN;

    uint8_t controlBytes[2] = {control >> 8, control & 0xFF};
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

int setConfigFilter(uint8_t filterNum, uint8_t filterType, uint16_t filterWord)
{
    uint32_t filter = 0;

    filter |= AD7124_FILT_REG_FILTER(filterType); // Filter type (e.g., Sinc4)
    filter |= AD7124_FILT_REG_FS(filterWord);     // Filter word

    uint8_t filterBytes[3] = 
    {
        (filter >> 16) & 0xFF,
        (filter >> 8) & 0xFF,
        filter & 0xFF
    };

    return adcWrite(AD7124_FILT0_REG + filterNum, filterBytes, 3);
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
    if (status != ESP_OK) 
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

void logRegisterRead(uint8_t reg, size_t len)
{
    uint8_t value[len];
    int status = adcRead(reg, value, len);

    if (status == ESP_OK) {
        printf("Register 0x%02X: ", reg);
        for (size_t i = 0; i < len; i++) {
            printf("0x%02X ", value[i]);
        }
        printf("\n");
    } else {
        printf("Failed to read register 0x%02X\n", reg);
    }
}

void ad7124_init()
{
    // Configure ADC control
    int status = setAdcControl(0x00, 0x03, 0x00, true); // Continuous, full power, internal clock, REF_EN = true
    printf("ADC control set status: %d\n", status);

    // Set configuration 
    status = setConfig(0, 0x02, 0x00, false); // config 0, Internal ref, Gain=1, Unipolar
    printf("ADC config set status: %d\n", status);

    // Set filter configuration 
    uint16_t filterWord = 0x0064; // filter word = 100
    status = setConfigFilter(0, 0x00, filterWord); // Sinc4 filter since its the default
    printf("ADC filter set status: %d\n", status);

    // Enable channel 0
    status = setChannel(0, 0, 0x00, 0x01, true); // AIN1 NEEDS TO BE SHORTED TO GROUND ON BOARD
    printf("Channel 0 set status: %d\n", status);

    // Enable only Channel 0
    status = enableChannel(0, true);
    printf("Channel 0 enable status: %d\n", status);

    logRegisterRead(AD7124_ADC_CTRL_REG, 2); // Log ADC control register
    logRegisterRead(AD7124_CFG0_REG, 2);     // Log config register 0
    logRegisterRead(AD7124_FILT0_REG, 3);    // Log filter register 0
    logRegisterRead(AD7124_CH0_MAP_REG, 2);  // Log channel 0 map register


    // Disable other channels
    for (int i = 1; i < 4; i++) 
    {
        status = enableChannel(i, false);
        printf("Channel %d disable status: %d\n", i, status);
    }
}

void logCriticalRegisters()
{
    uint8_t data[3];

    adcRead(AD7124_ADC_CTRL_REG, data, 2);  // Control Register
    printf("ADC Control Register: 0x%02X 0x%02X\n", data[0], data[1]);

    adcRead(AD7124_CFG0_REG, data, 2);      // Config Register 0
    printf("Config Register 0: 0x%02X 0x%02X\n", data[0], data[1]);

    adcRead(AD7124_FILT0_REG, data, 3);     // Filter Register 0
    printf("Filter Register 0: 0x%02X 0x%02X 0x%02X\n", data[0], data[1], data[2]);

    adcRead(AD7124_CH0_MAP_REG, data, 2);  // Channel 0 Map Register
    printf("Channel 0 Map Register: 0x%02X 0x%02X\n", data[0], data[1]);
}

// MQTT variables
static esp_mqtt_client_handle_t client;
static bool mqtt_connected = false;

// Wi-Fi Event Handler
static void wifi_event_handler(void *arg, esp_event_base_t event_base, int32_t event_id, void *event_data) {
    if (event_base == WIFI_EVENT && event_id == WIFI_EVENT_STA_START) {
        esp_wifi_connect();
    } else if (event_base == WIFI_EVENT && event_id == WIFI_EVENT_STA_DISCONNECTED) {
        ESP_LOGI(TAG, "Wi-Fi disconnected, reconnecting...");
        esp_wifi_connect();
    } else if (event_base == IP_EVENT && event_id == IP_EVENT_STA_GOT_IP) {
        ip_event_got_ip_t *event = (ip_event_got_ip_t *)event_data;
        ESP_LOGI(TAG, "Got IP Address: " IPSTR, IP2STR(&event->ip_info.ip));
    }
}

// Initialize Wi-Fi
static void wifi_init(void) {
    esp_netif_init();
    esp_event_loop_create_default();
    esp_netif_create_default_wifi_sta();

    wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();
    esp_wifi_init(&cfg);

    esp_event_handler_register(WIFI_EVENT, ESP_EVENT_ANY_ID, &wifi_event_handler, NULL);
    esp_event_handler_register(IP_EVENT, IP_EVENT_STA_GOT_IP, &wifi_event_handler, NULL);

    wifi_config_t wifi_config = {
        .sta = {
            .ssid = "Student5",    // ENTER YOUR WIFI USERNAME AND PASSWORD HERE
            .password = "Go Chargers!"
        },
    };

    esp_wifi_set_mode(WIFI_MODE_STA);
    esp_wifi_set_config(WIFI_IF_STA, &wifi_config);
    esp_wifi_start();
}

// MQTT Event Handler
static void mqtt_event_handler(void *handler_args, esp_event_base_t base, int32_t event_id, void *event_data) {
    esp_mqtt_event_handle_t event = (esp_mqtt_event_handle_t)event_data;

    switch ((esp_mqtt_event_id_t)event_id) {
    case MQTT_EVENT_CONNECTED:
        ESP_LOGI(TAG, "MQTT Connected");
        mqtt_connected = true;
        break;

    case MQTT_EVENT_DISCONNECTED:
        ESP_LOGI(TAG, "MQTT Disconnected");
        mqtt_connected = false;
        break;

    case MQTT_EVENT_PUBLISHED:
        ESP_LOGI(TAG, "Message Published: msg_id=%d", event->msg_id);
        break;

    case MQTT_EVENT_ERROR:
        ESP_LOGE(TAG, "MQTT Error");
        break;

    default:
        ESP_LOGI(TAG, "Unhandled MQTT Event: %" PRId32, event_id);
        break;
    }
}

// MQTT Initialization
void mqtt_app_start(void) {
    esp_mqtt_client_config_t mqtt_cfg = {
        .broker.address.uri = "mqtt://10.4.175.17", // ENTER THE IP OF THE MACHINE RUNNING THE SERVER HERE
        .broker.address.port = 1883
    };

    client = esp_mqtt_client_init(&mqtt_cfg);
    esp_mqtt_client_register_event(client, ESP_EVENT_ANY_ID, mqtt_event_handler, client);
    esp_mqtt_client_start(client);
}

void app_main(void) {
    // Initialize NVS
    esp_err_t ret = nvs_flash_init();
    if (ret == ESP_ERR_NVS_NO_FREE_PAGES || ret == ESP_ERR_NVS_NEW_VERSION_FOUND) {
        ESP_ERROR_CHECK(nvs_flash_erase());
        ret = nvs_flash_init();
    }
    ESP_ERROR_CHECK(ret);

    // Initialize Wi-Fi
    wifi_init();

    // Start MQTT
    mqtt_app_start();

    // SPI and ADC initialization
    spi_init();
    vTaskDelay(1000 / portTICK_PERIOD_MS);

    reset();
    vTaskDelay(1000 / portTICK_PERIOD_MS);

    ad7124_init();
    vTaskDelay(500 / portTICK_PERIOD_MS);

    // Reset and initialize OpenLog
    reset_openlog();
    openlog_uart_init();


    // ADC Data Sampling and Logging Loop
    while (1) {
        int32_t adcValue = 0;
        if (readData(&adcValue) == ESP_OK) {
            // Convert ADC value to voltage
            double voltage = toVoltage(adcValue, 1, 2.5, false);
            printf("ADC Value: %ld, Voltage: %.6f V\n", adcValue, voltage);

            // Get the current timestamp for OpenLog
            time_t now;
            time(&now);
            struct tm timeinfo;
            localtime_r(&now, &timeinfo);

            char timestamp[20];
            strftime(timestamp, sizeof(timestamp), "%Y-%m-%d %H:%M:%S", &timeinfo);

            // Prepare log line for OpenLog (with timestamp and voltage)
            char log_line[100];
            snprintf(log_line, sizeof(log_line), "%s, %g\n", timestamp, voltage);

            // Publish voltage to MQTT 
            if (mqtt_connected) 
            {
                char message[20];
                snprintf(message, sizeof(message), "%g", voltage);
                esp_mqtt_client_publish(client, "esp32/sensor/data", message, 0, 1, 0);
                ESP_LOGI(TAG, "Published to MQTT: %s", message);
            } 
            else 
            {
                ESP_LOGW(TAG, "MQTT not connected, skipping publish");
            }

            // Log to OpenLog
            log_to_openlog(log_line);
        } else {
            printf("Error reading ADC data\n");
        }

        // Delay for next sample
        vTaskDelay(10 / portTICK_PERIOD_MS);  // Adjust as needed
    }
}