import paho.mqtt.client as mqtt
import time
import socket
import csv
import requests
from datetime import datetime

def get_ip_address():
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.connect(("192.168.1.1", 80))
        ip_address = s.getsockname()[0]
        s.close()
        return ip_address
    except Exception as e:
        return f"Unable to get IP address: {e}"

# Print Local IP
ip_address = get_ip_address()
print(f"Device IP Address: {ip_address}")

# MQTT setup
broker = "localhost"
esp32_topic = "esp32/sensor/data"  # Topic the ESP32 publishes to
republish_topic = "sensor/data"  # Topic to republish to
command_topic = "desktop/commands"  # Topic for receiving commands from the Fam_app

# Create a CSV file with the current date as its name
current_date = datetime.now().strftime("%Y_%m_%d")
csv_file_name = f"{current_date}.csv"

with open(csv_file_name, mode='w', newline='') as file:
    writer = csv.writer(file)
    writer.writerow(["Timestamp", "Voltage_mV"])

# Function to fetch and save NASA DONKI API data
def fetch_and_save_donki_data():
    try:
        api_endpoint = "https://api.nasa.gov/DONKI/GST"
        api_key = "HSkpffGNq4SeOzWBlVvevS45zB5HUkX75g2uPmbO"  # API key from NASA
        start_date = "2024-01-01"
        end_date = "2024-12-31"

        # Build the request URL
        url = f"{api_endpoint}?startDate={start_date}&endDate={end_date}&api_key={api_key}"

        # Fetch the data
        response = requests.get(url)
        response.raise_for_status()

        print(f"HTTP Status: {response.status_code}")
        data = response.json()
        donki_csv_file_name = f"{datetime.now().strftime('%Y_%m_%d')}_api_donki.csv"

        # Save data to CSV
        with open(donki_csv_file_name, mode='w', newline='') as file:
            writer = csv.writer(file)
            writer.writerow(["gstID", "Start Time", "KP Index", "Link"])  # Header
            for event in data:
                gstID = event.get("gstID", "N/A")
                start_time = event.get("startTime", "N/A")
                kp_index = ", ".join(str(kp.get("kpIndex", "N/A")) for kp in event.get("allKpIndex", []))
                link = event.get("link", "N/A")
                writer.writerow([gstID, start_time, kp_index, link])

        print(f"Geomagnetic storm data saved to {donki_csv_file_name}")

        # Publish data to the MQTT topic api/data
        publish_csv_to_mqtt(donki_csv_file_name, "api/data")

    except requests.RequestException as e:
        print(f"Error fetching NASA DONKI API data: {e}")
    except Exception as e:
        print(f"Error processing NASA DONKI API data: {e}")

# Callback function to handle received messages
def on_message(client, userdata, msg):
    message = msg.payload.decode()
    print(f"Received on {msg.topic}: {message}")

    # Check what command
    if msg.topic == command_topic and message.lower() == "live":
        print("Received 'live' command. Outputting today's CSV data.")
        publish_csv_to_mqtt(csv_file_name, "sensor/data")
    elif msg.topic == command_topic and message.lower() == "fetch_donki":
        fetch_and_save_donki_data()
    elif msg.topic == esp32_topic:
        # Write Data to CSV
        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        formatted_message = f"{timestamp},{message}"
        
        with open(csv_file_name, mode='a', newline='') as file:
            writer = csv.writer(file)
            writer.writerow([timestamp, message])
            print(f"Written to {csv_file_name}: {timestamp}, {message}")

        # Republish the received data
        client.publish(republish_topic, formatted_message)
        print(f"Republished to {republish_topic}: {formatted_message}")


def publish_csv_to_mqtt(file_name, mqtt_topic):
    try:
        with open(file_name, mode='r') as file:
            reader = csv.reader(file)
            next(reader)  # Skip the header row
            for row in reader:
                message = ", ".join(row)
                client.publish(mqtt_topic, message)
                print(f"Published to {mqtt_topic}: {message}")
    except FileNotFoundError:
        print(f"File {file_name} not found. Cannot publish data.")
    except Exception as e:
        print(f"Error publishing data to MQTT topic: {e}")

client = mqtt.Client()
client.on_message = on_message
client.connect(broker)
client.subscribe(esp32_topic)
client.subscribe(command_topic)
client.loop_forever()
