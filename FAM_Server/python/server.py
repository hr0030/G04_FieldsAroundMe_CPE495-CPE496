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
        api_key = "HSkpffGNq4SeOzWBlVvevS45zB5HUkX75g2uPmbO"  # API key from NASA(Tyler)
        now = datetime.now()
        start_date = "2024-01-01" #now.replace(day=1).strftime("%Y-%m-%d")
        end_date = "2024-12-31" #now.strftime("%Y-%m-%d")

        # Build the request URL
        url = f"{api_endpoint}?startDate={start_date}&endDate={end_date}&api_key={api_key}"

    # Fetch the data
        response = requests.get(url)
        response.raise_for_status()  

        print(f"{response.status_code}")
        print(f"{response.text}")
        
        data = response.json()
        donki_csv_file_name = f"{datetime.now().strftime('%Y_%m_%d')}_api_donki.csv"
        
        # Output the fetched data
        print(f"Geomagnetic storm data from {start_date} to {end_date}:")
        for event in data:
            print(f"ID: {event['gstID']}")
            print(f"Start Time: {event['startTime']}")
            print(f"KP Index: {event.get('kpIndex', 'N/A')}")
            print(f"Link: {event['link']}\n")
    except requests.RequestException as e:
        print(f"Error fetching NASA DONKI API data: {e}")
    except Exception as e:
        print(f"Error processing NASA DONKI API data: {e}")

# Function to handle "live" command
def handle_live_command():
    print("Received 'live' command. Outputting today's CSV data.")
    try:
        # Open the CSV file and print its contents
        with open(csv_file_name, mode='r') as file:
            reader = csv.reader(file)
            next(reader)  # Skip the header
            print(f"Data from {csv_file_name}:")
            for row in reader:
                print(', '.join(row))
    except FileNotFoundError:
        print(f"No data file found for {csv_file_name}. Cannot output data.")

# Callback function to handle received messages
def on_message(client, userdata, msg):
    message = msg.payload.decode()
    print(f"Received on {msg.topic}: {message}")

    # Check what command
    if msg.topic == command_topic and message.lower() == "live":
        handle_live_command()
    elif msg.topic == command_topic and message.lower() == "fetch_donki":
        fetch_and_save_donki_data()
    else:
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

client = mqtt.Client()
client.on_message = on_message
client.connect(broker)
client.subscribe(esp32_topic)
client.subscribe(command_topic)
client.loop_forever()
