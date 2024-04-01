using System;
using System.IO;
using System.Net;

class Program
{
    static string ftpServer = "ftp://162.220.164.98";
    static string username = "poscentral@etr.solutions.com";
    static string password = "ho+Newt46";
    static string remoteFolderPath = "/Test 1";
    static string localFolderPath = @"D:\FTPfile";

    static void Main(string[] args)
    {

        FtpClient ftpClient = new FtpClient(ftpServer, username, password);

        DateTime startTime = DateTime.Now; 

        Console.WriteLine($"Application started at: {startTime}");

        recursiveDirectory(localFolderPath, remoteFolderPath, ftpClient);

        TimeSpan processingTime = DateTime.Now - startTime; 

        string formattedTime = $"{processingTime.Days} days, {processingTime.Hours} hours, {processingTime.Minutes} minutes, {processingTime.Seconds} seconds";

        Console.WriteLine($"Upload complete. Total processing time: {formattedTime}");
    }

    static void recursiveDirectory(string dirPath, string uploadPath, FtpClient ftpClient)
    {
        string[] files = Directory.GetFiles(dirPath, "*.*");
        string[] subDirs = Directory.GetDirectories(dirPath);


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


            recursiveDirectory(subDir, subDirRemotePath, ftpClient);
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

        using (Stream fileStream = File.OpenRead(localPath))
        using (Stream ftpStream = request.GetRequestStream())
        {
            fileStream.CopyTo(ftpStream);
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