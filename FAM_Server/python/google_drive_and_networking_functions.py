from googleapiclient.discovery import build
from googleapiclient.http import MediaFileUpload
from google.oauth2 import service_account
import socket
from datetime import datetime
import csv


SERVICE_ACCOUNT_FILE = 'Settings/fieldsaroundme-server-key.json'

SCOPES = ['https://www.googleapis.com/auth/drive.file']

def get_ip_address():
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.connect(("192.168.1.1", 80))
        ip_address = s.getsockname()[0]
        s.close()
        return ip_address
    except Exception as e:
        return f"Unable to get IP address: {e}"

def google_drive_upload(file_path, folder_id=None):
    try:
        creds = service_account.Credentials.from_service_account_file(
            SERVICE_ACCOUNT_FILE, scopes=SCOPES)
        service = build('drive', 'v3', credentials=creds)

 
        file_metadata = {
            'name': file_path.split('/')[-1],  
            'mimeType': 'text/csv'
        }

        if folder_id:
            file_metadata['parents'] = [folder_id]

        media = MediaFileUpload(file_path, mimetype='text/csv')
        file = service.files().create(
            body=file_metadata,
            media_body=media,
            fields='id'
        ).execute()

        print(f"File uploaded successfully. File ID: {file['id']}")

        permissions = {
            'type': 'anyone',
            'role': 'reader',  
        }


        service.permissions().create(
            fileId=file['id'],
            body=permissions,
            fields='id'
        ).execute()

        print("File permissions updated.")
        return file['id']

    except Exception as e:
        print(f"An error occurred: {e}")
        return None
