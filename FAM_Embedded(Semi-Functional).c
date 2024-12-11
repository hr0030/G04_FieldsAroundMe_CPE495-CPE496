// Prototype Embedded Code
// Jacob Dodd & Hayden Rose

#include <stdio.h>
#include <string.h>
#include "esp_err.h"
#include "esp_log.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "driver/spi_common.h"
#include "driver/spi_master.h"
#include "hal/spi_types.h"
#include "sdkconfig.h"
#include "AD_Defs.h"
#include "esp_mac.h"

// TODO:
// Fix up main function to test 
 
spi_device_handle_t handle;
 
uint8_t txData[4] = {0};
//uint8_t rxData[4] = {0};
 
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
        .clock_speed_hz = 614400,
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
        .rx_buffer = NULL,            // No data expected to be received
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
    uint8_t rxBuffer[3]; // 24-bit ADC data

    int ret = spiRead(txBuffer, rxBuffer, sizeof(rxBuffer) + 1);
    if (ret != ESP_OK) {
        return ret;
    }

    // Combine the 3 bytes into a 24-bit signed value
    *pData = (rxBuffer[0] << 16) | (rxBuffer[1] << 8) | rxBuffer[2];

    // Sign-extend if necessary
    if (*pData & 0x800000) {
        *pData |= 0xFF000000;
    }

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
        voltage = voltage / (double)0x7FFFFF - 1.0;
    } 
    else 
    {
        // Unipolar mode(FALL): scale the ADC value
        voltage = voltage / (double)0xFFFFFF;
    }

    // Apply reference voltage and gain scaling
    voltage = voltage * vref / (double)gain;

    return voltage;
}

int setAdcControl(uint8_t mode, uint8_t power, uint8_t clkSource, bool enable)
{
    uint16_t control = 0;

    control |= AD7124_ADC_CTRL_REG_MODE(mode);
    control |= AD7124_ADC_CTRL_REG_POWER_MODE(power);
    control |= AD7124_ADC_CTRL_REG_CLK_SEL(clkSource);
    if (enable) 
    {
        control |= AD7124_ADC_CTRL_REG_REF_EN;
    }

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

void app_main(void)
{
    // SPI and ADC initialization
    spi_init();
    vTaskDelay(1000 / portTICK_PERIOD_MS);

    reset();
    vTaskDelay(1000 / portTICK_PERIOD_MS);

    ad7124_init();
    vTaskDelay(500 / portTICK_PERIOD_MS);

    uint8_t txData[1];
    uint8_t rxData[8];

    // Read ID Register
    txData[0] = 0x45;   // ID register
    spiRead(txData, rxData, 2);
    printf("ID Register: 0x%02X\n", rxData[1]);

    // Read COMMS Register
    txData[0] = 0x40;   // COMMS register
    spiRead(txData, rxData, 2);
    printf("COMMS Register: 0x%02X\n", rxData[1]);

    // Read Error Register
    txData[0] = 0x46;   // Error register
    spiRead(txData, rxData, 3);
    printf("Error Register: 0x%02X\n", rxData[1]);

    // Read Channel 0 Register
    txData[0] = 0x49;   // CHANNEL_0 register
    spiRead(txData, rxData, 3);
    printf("Channel 0 Register: 0x%02X\n", rxData[1]);

    // ADC Data Sampling Loop
    while (1) {
        int32_t adcValue = 0;
        if (readData(&adcValue) == ESP_OK) {
            double voltage = toVoltage(adcValue, 1, 2.5, false);
            printf("ADC Value: %ld, Voltage: %.6f V\n", adcValue, voltage);
        } else {
            printf("Error reading ADC data\n");
        }
        vTaskDelay(1000 / portTICK_PERIOD_MS);
    }
}


