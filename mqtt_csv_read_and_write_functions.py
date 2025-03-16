# Function to write messages from "desktop/commands" to a file until "eof" is received
def write_mqtt_to_file(filepath, parserToUse):
    def on_message(client, userdata, msg):
        message = msg.payload.decode().strip()

        if message.upper() == "EOF":
            print("Received 'EOF'. Stopping data collection.")
            client.disconnect()
            return

        with open(filepath, "a") as file:
            if parserToUse == "UAH_SWIRLL":
                parsed = parse_UAH_SWIRLL(message)
                if parsed:
                    file.write(",".join(parsed) + "\n")
            else:
                file.write(message + "\n")

    command_client = mqtt.Client()
    command_client.on_message = on_message
    command_client.connect(broker)
    command_client.subscribe(command_topic)

    print(f"Listening to '{command_topic}' and writing to {filepath}. Waiting for 'eof' to stop.")
    command_client.loop_forever()


def publish_csv_date_range(folder_name, filename, start_date, end_date, mqtt_topic, column_number):
    try:
        start = datetime.datetime.strptime(start_date, "%Y-%m-%d")
        end = datetime.datetime.strptime(end_date, "%Y-%m-%d")

        while start <= end:
            date_str = f"{start.yeat}_{start.month}_{start.day}"
            full_filename = f"{folder_name}/{date_str}_{filename}.csv"
            if os.path.exists(full_filename):
                print(f"File '{full_filename}' exists.")
                publish_csv_to_mqtt(full_filename, mqtt_topic, column_number)
            else:
                print(f"File '{full_filename}' does not exist.")

            start += datetime.timedelta(days=1)

    except Exception as e:
        print(f"Error in publish_csv_date_range: {e}")


# Function to publish CSV file content to MQTT
def publish_csv_to_mqtt(file_name, mqtt_topic, column_number):
    try:
        with open(file_name, mode='r') as file:
            reader = csv.reader(file)
            next(reader)  # Skip the header row
            for row in reader:
                if len(row) > column_number:
                    message = f"{row[0]}, {row[column_number]}"
                    client.publish(mqtt_topic, message)
                    print(f"Published to {mqtt_topic}: {message}")
                else:
                    print(f"Skipping row {row} due to insufficient columns.")
    except FileNotFoundError:
        print(f"File {file_name} not found. Cannot publish data.")
    except Exception as e:
        print(f"Error publishing data to MQTT topic: {e}")