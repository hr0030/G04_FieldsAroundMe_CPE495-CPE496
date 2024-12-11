import paho.mqtt.client as mqtt

# MQTT setup
broker = input("Enter the MQTT IP address: ")
port = 1883  # Default MQTT port
topic = input("Enter the topic to publish to: ")


client = mqtt.Client()

# Connect to the MQTT broker
try:
    client.connect(broker, port)
    print(f"Connected to MQTT broker at {broker}:{port}")
except Exception as e:
    print(f"Failed to connect: {e}")
    exit()

# publish data
def publish_user_data():
    while True:
        user_input = input("Enter message: ")
        if user_input.lower() == "exit":
            print("Exiting...")
            break
        client.publish(topic, user_input)
        print(f"Published: {user_input}")

publish_user_data()
client.disconnect()
print("Disconnected from MQTT broker.")
