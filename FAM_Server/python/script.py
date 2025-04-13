'''

import requests
import csv
import json
from datetime import datetime


try:
        api_endpoint = "https://api.nasa.gov/DONKI/FLR"
        api_key = "HSkpffGNq4SeOzWBlVvevS45zB5HUkX75g2uPmbO"  # API key from NASA
        start_date = "2024-10-01"
        end_date = "2025-01-01"

        # Build the request URL
        url = f"{api_endpoint}?startDate={start_date}&endDate={end_date}&api_key={api_key}"

        # Fetch the data
        response = requests.get(url)
        response.raise_for_status()

        print(f"HTTP Status: {response.status_code}")
        data = response.json()
        donki_gst_csv_file_name = f"FLR_donki_{start_date}_{end_date}.csv"

        # Save data to CSV
        with open(donki_gst_csv_file_name, mode='w', newline='') as file:
            writer = csv.writer(file)
            writer.writerow(["Timestamp", "Speed"])  # Header

            for event in data:
                start_time = event.get("peakTime", "N/A")

                # Ensure timestamp is correctly formatted
                try:
                    timestamp = datetime.strptime(start_time, "%Y-%m-%dT%H:%MZ")
                    formatted_timestamp = timestamp.strftime("%Y-%m-%d %H:%M:%S")
                except ValueError:
                    print(f"Invalid timestamp format: {start_time}")
                    continue

                # Extract and average KP Index values
                # kp_values = [kp.get("speed") for kp in event.get("cmeAnalyses", []) if isinstance(kp.get("speed"), (int, float))]
                kp_values = [event.get("classType")]
                if not kp_values:
                    print(f"No valid KP index data for event at {start_time}")
                    continue

                writer.writerow([formatted_timestamp, kp_values])

except requests.RequestException as e:
        print(f"Error fetching NASA DONKI API data: {e}")
except Exception as e:
        print(f"Error processing NASA DONKI API data: {e}")
        '''
import csv
import datetime


def parse_UAH_SWIRLL(parts):
    if len(parts) < 16:
        return None  # Skip malformed lines

    year = parts[1]
    julian_day = parts[2]
    hour_minute = int(parts[3])
    hours = hour_minute // 100
    minutes = hour_minute % 100
    seconds = parts[4]

    try:
        date_time = datetime.datetime.strptime(
            f"{year} {julian_day} {hours}:{minutes}:{seconds}",
            "%Y %j %H:%M:%S"
        ).strftime("%Y-%m-%d %H:%M:%S")

    except ValueError as e:
        print(f"Skipping invalid datetime: {e}")
        return None

    pressure = parts[5]
    humidity = parts[7]
    solar_radiation = parts[11]

    return [date_time, solar_radiation]



def feed_csv_to_parser_and_write(input_filename, writer):
    with open(input_filename, 'r', newline='') as infile:
        reader = csv.reader(infile)
        for line in reader:
            parsed = parse_UAH_SWIRLL(line)
            if parsed:
                writer.writerow(parsed)

def batch_feed_csvs(start_date, end_date, output_filename):
    current_date = start_date

    with open(output_filename, 'w', newline='') as outfile:
        writer = csv.writer(outfile)

        while current_date <= end_date:
            input_filename = current_date.strftime('%Y-%m-%d') + '.txt'
            try:
                print(f'Processing: {input_filename}')
                feed_csv_to_parser_and_write(input_filename, writer)
            except FileNotFoundError:
                print(f'Skipped missing file: {input_filename}')
            current_date += datetime.timedelta(days=1)

# Example usage:
start = datetime.date(2024, 11, 1)
end = datetime.date(2025, 3, 1)
batch_feed_csvs(start, end, 'combined_output.csv')
