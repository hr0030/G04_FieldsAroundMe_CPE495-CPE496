// Prototype Embedded Code
// Jacob Dodd & Hayden Rose

#include <stdio.h>
#include "esp_err.h"
#include "esp_log.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "driver/spi_common.h"
#include "driver/spi_master.h"
#include "hal/spi_types.h"
#include "sdkconfig.h"
 
spi_device_handle_t handle;
 
uint8_t txData[4] = {0};
//uint8_t rxData[4] = {0};
 
static void spi_init()
{
    spi_bus_config_t buscfg = {
        
        .miso_io_num = 20, // 22, // 12,
        .mosi_io_num = 19, // 21, // 13,
        .sclk_io_num = 21, // 17, // 14,
        .quadwp_io_num = -1,
        .quadhd_io_num = -1,
        .max_transfer_sz = 32,
        .flags = 0,
    };
 
    ESP_ERROR_CHECK(spi_bus_initialize(SPI2_HOST, &buscfg, SPI_DMA_DISABLED)); // SPI_DMA_CH_AUTO
 
    spi_device_interface_config_t devcfg = {
        .address_bits = 0,//8, // set if using .addr in transaction
        .command_bits = 0, // set if using .cmd in transaction
        .dummy_bits = 0,
        .mode = 3,
        .duty_cycle_pos = 0,
        .cs_ena_posttrans = 0,
        .cs_ena_pretrans = 0,
        .clock_speed_hz = 614400,
        .spics_io_num = -1,
        .flags = 0, // SPI_DEVICE_HALFDUPLEX,
        .queue_size = 1,
        .pre_cb = NULL,
        .post_cb = NULL,
    };
    ESP_ERROR_CHECK(spi_bus_add_device(SPI2_HOST, &devcfg, &handle));
};
 
static int spiRead(uint8_t *_data, int len)
{
    esp_err_t intError = 0;
    spi_transaction_t trans_desc;
    uint8_t rxData[4] = {0};
    int32_t value = 0;
 
    printf("address %x\n", _data[0]);
 
    trans_desc.addr = 0,//_data[0];
    trans_desc.cmd = 0;
    trans_desc.flags = 0, // SPI_TRANS_USE_RXDATA | SPI_TRANS_USE_TXDATA;
        trans_desc.user = NULL,
    trans_desc.length = 8 * len; // total data bits, sent and received
        //trans_desc.tx_buffer = NULL; // if using .addr
    trans_desc.tx_buffer = _data;
    //  trans_desc.tx_data[0] = _data[0];
    //  trans_desc.tx_data[1] = _data[1];
    //  trans_desc.tx_data[2] = _data[2];
    //  trans_desc.tx_data[3] = _data[3];
    trans_desc.rxlength = 8 * (len-1);
    trans_desc.rx_buffer = rxData;
    // trans_desc.rx_data = NULL;
 
    intError = spi_device_transmit(handle, &trans_desc);
 
    printf("rx data %x\n", rxData[0]);
    // printf("rx data2 %x\n\n", trans_desc.rx_data[0]);
 
    /* Build the result */
    for (int i = 1; i < len + 1; i++)
    {
        value <<= 8;
        value += rxData[i];
    }
    printf("value %lx\n\n", value);
 
    if (intError != 0)
    {
        printf("error %d\n", intError);
    }
    return intError;
}
 
static int spiWrite(uint8_t *_data)
{
    esp_err_t intError;
    spi_transaction_t trans_desc;
 
    trans_desc.addr = 0;
    trans_desc.cmd = 0;
    trans_desc.flags = 0;
    trans_desc.length = 32; // total data bits
    trans_desc.tx_buffer = _data;
    trans_desc.rxlength = 32;
    trans_desc.rx_buffer = _data;
 
    intError = spi_device_transmit(handle, &trans_desc);
 
    if (intError != 0)
    {
        printf("error %d\n", intError);
    }
    return intError;
}
 
void reset()
{
    // need to write 64 1's to reset, can do this with address phase, command phase only supports 16 bit and write 32 bit?
    uint8_t wr_buf[8] = {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};
    spiWrite(wr_buf);
    spiWrite(wr_buf);
}

void app_main(void)
{
    spi_init();
    vTaskDelay(1000 / portTICK_PERIOD_MS);
 
    reset();
    vTaskDelay(100 / portTICK_PERIOD_MS);
 
    txData[0] = 0x45;   // ID register
    spiRead(txData, 3); // should return 0x14
    vTaskDelay(100 / portTICK_PERIOD_MS);
 



    txData[0] = 0x40;   // COMMS register
    spiRead(txData, 3); // should return 0x00
    vTaskDelay(100 / portTICK_PERIOD_MS);
 
    txData[0] = 0x46;   // Error register
    spiRead(txData, 3); // should return 0x00
 
    txData[0] = 0x49;   // CHANNEL_0 register
    spiRead(txData, 3); // should return 0x8001
}
