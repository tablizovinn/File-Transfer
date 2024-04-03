using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net.Http;

class Program
{
    static string ftpServer = "ftp://162.220.164.98";
    static string username = "poscentral@etr.solutions.com";
    static string password = "ho+Newt46";
    static string remoteFolderPath = "/Test 1";
    static string localFolderPath = @"D:\FTPfile";
    static string apiUrl = "https://localhost:7123/api/v1/pulse";

    static async Task Main(string[] args)
    {
        FtpClient ftpClient = new FtpClient(ftpServer, username, password);

        DateTime startTime = DateTime.Now;
        Console.WriteLine($"Application started at: {startTime}");

        string computerName = Environment.MachineName;
        Console.WriteLine($"Computer name: {computerName}");

        Console.WriteLine("Enter Task ID:");
        string taskId = Console.ReadLine();

        await CreatePulse(apiUrl, computerName, taskId, "Starting");

        await UploadFilesParallel(localFolderPath, remoteFolderPath, ftpClient, computerName, taskId);

        await CreatePulse(apiUrl, computerName, taskId, "Finished");

        TimeSpan processingTime = DateTime.Now - startTime;
        string formattedTime = $"{processingTime.Days} days, {processingTime.Hours} hours, {processingTime.Minutes} minutes, {processingTime.Seconds} seconds";

        Console.WriteLine($"Total processing time: {formattedTime}");
    }

    static async Task UploadFilesParallel(string dirPath, string uploadPath, FtpClient ftpClient, string computerName, string taskId)
    {
        await CreatePulse(apiUrl, computerName, taskId, "Uploading");

        string[] files = Directory.GetFiles(dirPath, "*.*");
        string[] subDirs = Directory.GetDirectories(dirPath);

        List<Task> uploadTasks = new List<Task>();

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            Console.WriteLine($"Queuing file for upload: {fileName}");
            uploadTasks.Add(UploadFileWithRetryAsync(ftpClient, uploadPath + "/" + Path.GetFileName(file), file));
        }

        foreach (string subDir in subDirs)
        {
            string subDirName = Path.GetFileName(subDir);
            string subDirRemotePath = uploadPath + "/" + subDirName;

            await ftpClient.CreateDirectoryWithRetryAsync(subDirRemotePath);
            await UploadFilesParallel(subDir, subDirRemotePath, ftpClient, computerName, taskId);
        }

        await Task.WhenAll(uploadTasks);
    }

    static async Task CreatePulse(string apiUrl, string computerName, string taskId, string status)
    {
        using (var httpClient = new HttpClient())
        {
            var requestData = new
            {
                computer = computerName,
                taskid = taskId,
                status = status
            };

            var jsonRequest = JsonConvert.SerializeObject(requestData);
            var requestBody = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(apiUrl, requestBody);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Pulse logs created successfully.");
            }
            else
            {
                Console.WriteLine($"Failed to create pulse logs. Status code: {response.StatusCode}");
            }
        }
    }

    static async Task UploadFileWithRetryAsync(FtpClient ftpClient, string remotePath, string localPath)
    {
        const int maxRetries = 5; // Increase the number of retry attempts
        const int delayMilliseconds = 2000; // Increase the delay between retries
        int retryCount = 0;

        while (true)
        {
            try
            {
                await ftpClient.UploadFileAsync(remotePath, localPath);
                break; // Upload successful, exit retry loop
            }
            catch (WebException ex) when (ex.Response is FtpWebResponse ftpResponse && ftpResponse.StatusCode == FtpStatusCode.ServiceNotAvailable)
            {
                retryCount++;
                if (retryCount > maxRetries)
                {
                    // Max retries reached, throw the exception
                    throw;
                }
                // Wait for a short delay before retrying
                await Task.Delay(delayMilliseconds);
            }
        }
    }
}

class FtpClient
{
    private string ftpServer;
    private string username;
    private string password;

    public FtpClient(string ftpServer, string username, string password)
    {
        this.ftpServer = ftpServer;
        this.username = username;
        this.password = password;
    }

    public async Task UploadFileAsync(string remotePath, string localPath)
    {
        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpServer + "/" + remotePath);
        request.Method = WebRequestMethods.Ftp.UploadFile;
        request.Credentials = new NetworkCredential(username, password);

        request.UseBinary = true;

        byte[] buffer = new byte[8192];
        int bytesRead;

        using (Stream fileStream = File.OpenRead(localPath))
        using (Stream ftpStream = await request.GetRequestStreamAsync())
        {
            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await ftpStream.WriteAsync(buffer, 0, bytesRead);
            }
        }
    }

    public async Task CreateDirectoryWithRetryAsync(string remotePath)
    {
        const int maxRetries = 1000000; // Increase the number of retry attempts
        const int delayMilliseconds = 2000; // Increase the delay between retries
        int retryCount = 0;

        while (true)
        {
            FtpWebResponse response = null;

            try
            {
                response = (FtpWebResponse)await CreateDirectoryAsync(remotePath);
                // Do something with the response if needed
                break; // Directory creation successful, exit retry loop
            }
            catch (WebException ex) when (ex.Response is FtpWebResponse ftpResponse && ftpResponse.StatusCode == FtpStatusCode.ServiceNotAvailable)
            {
                retryCount++;
                if (retryCount > maxRetries)
                {
                    // Max retries reached, throw the exception
                    throw;
                }
                // Wait for a short delay before retrying
                await Task.Delay(delayMilliseconds);
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
        }
    }

    private async Task<FtpWebResponse> CreateDirectoryAsync(string remotePath)
    {
        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpServer + "/" + remotePath);
        request.Method = WebRequestMethods.Ftp.MakeDirectory;
        request.Credentials = new NetworkCredential(username, password);

        return (FtpWebResponse)await request.GetResponseAsync();
    }
}
