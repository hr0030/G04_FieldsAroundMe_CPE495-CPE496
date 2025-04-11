using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Drive.v3.Data;
using System.Diagnostics;

public class Cloud_Functions
{

    private static string SERVICE_ACCOUNT_FILE = "fieldsaroundme_desktop_one_key.json"; 
    private static string[] SCOPES = { DriveService.Scope.DriveFile };
    public static string UploadFileToGoogleDrive(string filePath)
    {
        try
        {

            GoogleCredential credential;
            using (var stream = new FileStream(SERVICE_ACCOUNT_FILE, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(SCOPES);
            }


            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "FAMApp"
            });


            string fileName = Path.GetFileName(filePath);

            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName, 
                MimeType = "text/csv"
            };

            FilesResource.CreateMediaUpload request;
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                request = service.Files.Create(fileMetadata, stream, "text/csv");
                request.Fields = "id";
                request.Upload();
            }

            var file = request.ResponseBody;
            Debug.WriteLine($"File uploaded successfully. File ID: {file.Id}, File Name: {fileName}");

            var permission = new Permission
            {
                Type = "anyone",
                Role = "reader"
            };

            service.Permissions.Create(permission, file.Id).Execute();
            Debug.WriteLine("File permissions updated.");

            return file.Id;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"An error occurred: {ex.Message}");
            return null;
        }
    }




    public static bool DownloadFileFromGoogleDrive(string fileName, string savePath)
    {
        try
        {
            GoogleCredential credential;
            using (var stream = new FileStream(SERVICE_ACCOUNT_FILE, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(SCOPES);
            }

            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "FAMApp"
            });

            var request = service.Files.List();
            request.Q = $"name = '{fileName}' and trashed = false";
            request.Fields = "files(id, name)";

            var response = request.Execute();
            if (response.Files.Count == 0)
            {
                Debug.WriteLine($"File '{fileName}' not found on Google Drive.");
                return false;
            }

            string fileId = response.Files[0].Id;

            var downloadRequest = service.Files.Get(fileId);
            using (var stream = new FileStream(Path.Combine(savePath, fileName), FileMode.Create, FileAccess.Write))
            {
                downloadRequest.Download(stream);
            }

            Debug.WriteLine($"File '{fileName}' downloaded successfully to {savePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"An error occurred: {ex.Message}");
            return false;
        }
    }

}
