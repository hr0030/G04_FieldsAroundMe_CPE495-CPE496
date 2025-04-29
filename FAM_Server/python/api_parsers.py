import time
import socket
import math
from datetime import datetime
import csv
import requests



def fetch_and_save_donki_gst_data(start_date, end_date):
    try:
        api_endpoint = "https://api.nasa.gov/DONKI/GST"
        api_key = "HSkpffGNq4SeOzWBlVvevS45zB5HUkX75g2uPmbO"  # API key from NASA

        # Correctly Format Timestamp
        start_date = start_date.replace('_', '-')
        end_date = end_date.replace('_', '-')
                
        # Build the request URL
        url = f"{api_endpoint}?startDate={start_date}&endDate={end_date}&api_key={api_key}"

        # Fetch the data
        response = requests.get(url)
        response.raise_for_status()
        print(url)
        print(f"HTTP Status: {response.status_code}")
        data = response.json()
        donki_gst_csv_file_name = f"APIs/donki_gst_api.csv"

        # Save data to CSV
        with open(donki_gst_csv_file_name, mode='w', newline='') as file:
            writer = csv.writer(file)
            writer.writerow(["Timestamp", "KP Index"])  # Header

            for event in data:
                start_time = event.get("startTime", "N/A")

                # Ensure timestamp is correctly formatted
                try:
                    timestamp = datetime.strptime(start_time, "%Y-%m-%dT%H:%MZ")
                    formatted_timestamp = timestamp.strftime("%Y-%m-%d %H:%M:%S")
                except ValueError:
                    print(f"Invalid timestamp format: {start_time}")
                    continue

                # Extract and average KP Index values
                kp_values = [kp.get("kpIndex") for kp in event.get("allKpIndex", []) if isinstance(kp.get("kpIndex"), (int, float))]
                if not kp_values:
                    print(f"No valid KP index data for event at {start_time}")
                    continue

                avg_kp_index = sum(kp_values) / len(kp_values)
                writer.writerow([formatted_timestamp, avg_kp_index])

        print(f"Geomagnetic storm data saved to {donki_gst_csv_file_name}")

    except requests.RequestException as e:
        print(f"Error fetching NASA DONKI API data: {e}")
    except Exception as e:
        print(f"Error processing NASA DONKI API data: {e}")


'''
def fetch_and_save_donki_slr_data(start_date, end_date):
    try:
        api_endpoint = "https://api.nasa.gov/DONKI/FLR"
        api_key = "HSkpffGNq4SeOzWBlVvevS45zB5HUkX75g2uPmbO"  # API key from NASA
        start_date = start_date.replace('_', '-')
        end_date = end_date.replace('_', '-')

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
def fetch_and_save_temperature_api(start_date, end_date):
            try:
                # Correctly Format Timestamp
                start_date = start_date.replace('_', '-')
                end_date = end_date.replace('_', '-')

                api_endpoint = "https://www.ncdc.noaa.gov/cdo-web/api/v2/data"
                api_key = "gcYwsQlKpUDOdqvTjKeflLqCAcuGyHdl"  # NOAA API Key(Probably Should Not be on public github)
                dataset_id = "GHCND"  # Global Historical Climatology Network Daily
                station_id = "GHCND:USW00003856"  #Huntsville Airport ID
                # start_date = "2025-01-01"
                # end_date = datetime.now().strftime('%Y-%m-%d')
                datatype_id = "TAVG"  # Average temperature
                limit = 500
                # lat = 34.7245529
                # lon = -86.6404766

                headers = {
                    "token": api_key,
                    "Accept": "application/json"
                }
                params = {
                    "datasetid": dataset_id,
                    "stationid": station_id,
                    "startdate": start_date,
                    "enddate": end_date,
                    "datatypeid": datatype_id,
                    "limit": limit,
                    "units": "metric"
                    # "lat": lat,
                    # "lon": lon
                }

                response = requests.get(api_endpoint, headers=headers, params=params)
                response.raise_for_status()
                data = response.json()
                results = data["results"]
                if not results:
                    print("No temperature data returned from NOAA.")
                    return

                # CSV file path
                noaa_temp_csv_file_name = f"APIs/noaa_temp.csv"

                # Save to CSV
                with open(noaa_temp_csv_file_name, mode='w', newline='') as file:
                    writer = csv.writer(file)
                    writer.writerow(["Date", "Temperature (°C)"])  # Header

                    for record in results:
                        print(f"Processing record: {record}")  # Debugging

                        date = record.get("date")
                        temperature = record.get("value")

                        if date is None or temperature is None:
                            print(f"Skipping invalid record: {record}")
                            continue

                        # Format date correctly
                        try:
                            formatted_date = datetime.strptime(date, "%Y-%m-%dT%H:%M:%S").strftime("%Y-%m-%d")
                        except ValueError:
                            print(f"Invalid date format: {date}")
                            continue

                        writer.writerow([formatted_date, temperature])

                print(f"NOAA temperature data saved to {noaa_temp_csv_file_name}")

            except requests.RequestException as e:
                print(f"Error fetching NOAA API data: {e}")
            except Exception as e:
                print(f"Error processing NOAA API data: {e}")

def parse_UAH_SWIRLL(line):
                parts = line.strip().split(',')
                if len(parts) < 16:
                    return None  # Skip malformed lines

                year = parts[1]
                julian_day = parts[2]
                hour_minute = int(parts[3])
                hours = math.floor(hour_minute/100)
                minutes = hour_minute - (hours * 100)
                seconds = parts[4]
                date_time = datetime.strptime(f"{year} {julian_day} {hours}:{minutes}:{seconds}", "%Y %j %H:%M:%S").strftime(
                    "%Y-%m-%d %H:%M:%S")

                pressure = parts[5]
                humidity = parts[7]
                solar_radiation = parts[11]

                return [date_time, pressure, humidity, solar_radiation]


def parse_Samsung_hr(line):
    parts = line.strip().split(',')
    if len(parts) < 21:
        return None  # skip incomplete/malformed lines

    try:
        # Get important fields
        start_time_str = parts[4].strip()  # 5th column: start_time
        heart_rate = parts[20].strip()     # 21st column: heart_rate

        if not start_time_str or not heart_rate:
            return None  # skip if missing important fields

        # Parse the timestamp correctly
        start_time = datetime.strptime(start_time_str.strip(), "%Y-%m-%d %H:%M:%S.%f")
        formatted_time = start_time.strftime("%Y-%m-%d %H:%M:%S")  # clean format (no milliseconds)
        # Prepare the return line
        return f"{formatted_time},{heart_rate.strip()}"

    except Exception as e:
        print(f"Error parsing line: {e}")
        return None


