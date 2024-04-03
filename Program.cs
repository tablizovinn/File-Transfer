using System;
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

       
        Guid taskId = Guid.NewGuid(); 


        await CreatePulse(apiUrl, computerName, taskId.ToString(), "Starting file upload");

        await recursiveDirectory(localFolderPath, remoteFolderPath, ftpClient, computerName, taskId.ToString());

        await CreatePulse(apiUrl, computerName, taskId.ToString(), "All files uploaded");

        TimeSpan processingTime = DateTime.Now - startTime;
        string formattedTime = $"{processingTime.Days} days, {processingTime.Hours} hours, {processingTime.Minutes} minutes, {processingTime.Seconds} seconds";

        Console.WriteLine($"Total processing time: {formattedTime}");
    }

    static async Task recursiveDirectory(string dirPath, string uploadPath, FtpClient ftpClient, string computerName, string taskId)
    {
        
        string[] files = Directory.GetFiles(dirPath, "*.*");
        string[] subDirs = Directory.GetDirectories(dirPath);

       
            
        await CreatePulse(apiUrl, computerName, taskId, "Uploading files and directories");
        

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            Console.WriteLine($"Uploading file: {fileName}");
            ftpClient.UploadFile(uploadPath + "/" + Path.GetFileName(file), file);
        }

        foreach (string subDir in subDirs)
        {
            string subDirName = Path.GetFileName(subDir);
            Console.WriteLine($"Uploading subdirectory: {subDirName}");
            string subDirRemotePath = uploadPath + "/" + subDirName;

            ftpClient.CreateDirectory(subDirRemotePath);
            await recursiveDirectory(subDir, subDirRemotePath, ftpClient, computerName, taskId);
        }
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

    public void UploadFile(string remotePath, string localPath)
    {
        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpServer + "/" + remotePath);
        request.Method = WebRequestMethods.Ftp.UploadFile;
        request.Credentials = new NetworkCredential(username, password);

        request.UseBinary = true;

        byte[] buffer = new byte[8192];
        int bytesRead;

        using (Stream fileStream = File.OpenRead(localPath))
        using (Stream ftpStream = request.GetRequestStream())
        {
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                ftpStream.Write(buffer, 0, bytesRead);
            }
        }
    }

    public void CreateDirectory(string remotePath)
    {
        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpServer + "/" + remotePath);
        request.Method = WebRequestMethods.Ftp.MakeDirectory;
        request.Credentials = new NetworkCredential(username, password);

        using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
        {

        }
    }
}
