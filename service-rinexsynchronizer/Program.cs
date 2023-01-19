using System;
using System.IO;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Text;

namespace RinexSynchronizer
{
    class Program
    {
        private static LogWriter lwSingleton = LogWriter.GetInstance;
        private static DateTime dateTime;
        private static string dateTimePath;
        private static Stopwatch stopwatchStep;
        private static Stopwatch stopwatchTotal;
        private static string dateDataShareShopPath = "";
        private static string dateDataShareNPRPath = "";
        private static List<string> archivePathList = new List<string>() { "DAFI", "NoShop", "Shop" };
        private static bool isDelete = false;
        private static DBHelper dbH;
        private static readonly string dbPath = ConfigurationManager.AppSettings["DBPath"];
        private static readonly string dbFile = ConfigurationManager.AppSettings["DBFile"];
        private static readonly string archivPathTPP01 = ConfigurationManager.AppSettings["ArchivPathTPP01"];
        private static readonly string archivPathTPP02 = ConfigurationManager.AppSettings["ArchivPathTPP02"];
        private static readonly string shopPathTPP01 = ConfigurationManager.AppSettings["ShopPathTPP01"];
        private static readonly string shopPathTPP02 = ConfigurationManager.AppSettings["ShopPathTPP02"];
        private static readonly string nprPathTPP01 = ConfigurationManager.AppSettings["NPRPathTPP01"];
        private static readonly string nprPathTPP02 = ConfigurationManager.AppSettings["NPRPathTPP02"];
        private static readonly string dataShareArchivePath = ConfigurationManager.AppSettings["DataShareArchivePath"];
        private static readonly string dataShareShopPath = ConfigurationManager.AppSettings["DataShareShopPath"];
        private static readonly string dataShareNPRPath = ConfigurationManager.AppSettings["DataShareNPRPath"];
        private static readonly string logFilesToCheck = ConfigurationManager.AppSettings["LogFilesToCheck"];
        private static bool checkPreviousLogs = bool.Parse(ConfigurationManager.AppSettings["CheckPreviousLogs"] ?? "true");

        static void Main(string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    Usage(args);
                }
                else
                {
                    if (!Directory.Exists(ConfigurationManager.AppSettings["LogPath"]))
                    {
                        Directory.CreateDirectory(ConfigurationManager.AppSettings["LogPath"]);
                    }
                    LogWriter.WriteToLog(string.Format("===============================Start=============================="));
                    Process processes = Process.GetCurrentProcess();
                    processes.PriorityClass = ProcessPriorityClass.Normal;
                    stopwatchStep = new Stopwatch();
                    stopwatchTotal = new Stopwatch();
                    if (CheckIfReady())
                    {
                        Processing();
                        Cleanup();
                    }
                    LogWriter.WriteToLog(string.Format("=================================End=============================="));
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
                LogWriter.WriteToLog(e);
                LogWriter.WriteToLog(string.Format("=================================End=============================="));
            }
            finally
            {
                LogWriter.ForceFlush();
            }

        }

        private static bool IsLogFileStatusOK(string logfile)
        {
            string[] lines = File.ReadAllLines(logfile, Encoding.UTF8);
            bool textBeforeEnd = lines[lines.Length - 2].Contains("Totaltime to process");
            bool hourBeforeEnd = lines[lines.Length - 2].StartsWith(DateTime.Now.ToString("HH"));
            bool textEnd = lines[lines.Length - 1].Contains("=================================End==============================");
            bool hourEnd = lines[lines.Length - 1].StartsWith(DateTime.Now.ToString("HH"));
            return textBeforeEnd && hourBeforeEnd && textEnd && hourEnd;
        }

        private static bool CheckIfReady()
        {
            if (!checkPreviousLogs)
            {
                LogWriter.WriteToLog(string.Format("INFORMATION: check of previous process skipped!"));
                return true;
            }
            else
            {
                int count = 0;
                string[] logFileToCheck = logFilesToCheck.Split(',');
                if (File.Exists(Path.Combine(ConfigurationManager.AppSettings["LogPathToCheck"], DateTime.Now.ToString("yyyy-MM-dd") + "_" + logFileToCheck[0])) &&
                    File.Exists(Path.Combine(ConfigurationManager.AppSettings["LogPathToCheck"], DateTime.Now.ToString("yyyy-MM-dd") + "_" + logFileToCheck[1])) &&
                    File.Exists(Path.Combine(ConfigurationManager.AppSettings["LogPathToCheck"], DateTime.Now.ToString("yyyy-MM-dd") + "_" + logFileToCheck[2])) &&
                    File.Exists(Path.Combine(ConfigurationManager.AppSettings["LogPathToCheck"], DateTime.Now.ToString("yyyy-MM-dd") + "_" + logFileToCheck[3])) &&
                    File.Exists(Path.Combine(ConfigurationManager.AppSettings["LogPathToCheck"], DateTime.Now.ToString("yyyy-MM-dd") + "_" + logFileToCheck[4])) &&
                    File.Exists(Path.Combine(ConfigurationManager.AppSettings["LogPathToCheck"], DateTime.Now.ToString("yyyy-MM-dd") + "_" + logFileToCheck[5])))
                {
                    bool stayInWhile = false;
                    do
                    {
                        bool statusDAFITPP01 = IsLogFileStatusOK(Path.Combine(ConfigurationManager.AppSettings["LogPathToCheck"], DateTime.Now.ToString("yyyy-MM-dd") + "_" + logFileToCheck[0]));
                        bool statusDAFITPP02 = IsLogFileStatusOK(Path.Combine(ConfigurationManager.AppSettings["LogPathToCheck"], DateTime.Now.ToString("yyyy-MM-dd") + "_" + logFileToCheck[1]));
                        bool statusNoShopTPP01 = IsLogFileStatusOK(Path.Combine(ConfigurationManager.AppSettings["LogPathToCheck"], DateTime.Now.ToString("yyyy-MM-dd") + "_" + logFileToCheck[2]));
                        bool statusNoShopTPP02 = IsLogFileStatusOK(Path.Combine(ConfigurationManager.AppSettings["LogPathToCheck"], DateTime.Now.ToString("yyyy-MM-dd") + "_" + logFileToCheck[3]));
                        bool statusShopTPP01 = IsLogFileStatusOK(Path.Combine(ConfigurationManager.AppSettings["LogPathToCheck"], DateTime.Now.ToString("yyyy-MM-dd") + "_" + logFileToCheck[4]));
                        bool statusShopTPP02 = IsLogFileStatusOK(Path.Combine(ConfigurationManager.AppSettings["LogPathToCheck"], DateTime.Now.ToString("yyyy-MM-dd") + "_" + logFileToCheck[5]));
                        if (statusDAFITPP01 && statusDAFITPP02 && statusNoShopTPP01 && statusNoShopTPP02 && statusShopTPP01 && statusShopTPP02)
                        {
                            stayInWhile = false;
                        }
                        else
                        {
                            stayInWhile = true;
                            System.Threading.Thread.Sleep(10000);
                        }
                        count++;
                    } while (stayInWhile && count < 60);
                    if (count == 60)
                    {
                        LogWriter.WriteToLog(string.Format("Timeout for processing! No synchronization done because previous process not ready."));
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    LogWriter.WriteToLog(string.Format("Not all logfiles for check exist! No synchronization done because previous process could not be checked."));
                    return false;
                }
            }
        }

        private static void Usage(string[] args)
        {

            if (args[0].Length > 0)
            {
                Console.WriteLine("Compares epoches of Rinex 3 and NPR files in two sources.");
                Console.WriteLine("Copies the files with more epoches in destination folder.");
                Console.WriteLine("");
                Console.WriteLine("RinexSynchronizer [/?] [/help]");
                Console.WriteLine("");
                Console.WriteLine("/? /help     Shows this help.");
                Console.WriteLine("");
                Console.WriteLine("No Parameter are needed to start the processing.");
                Console.WriteLine("All configurations are read from the .config file.");
                Console.WriteLine("The configuration for RinexSynchronizer can be found at the install folder (.config files).");
            }
        }

        private static void Processing()
        {
            stopwatchTotal.Start();
            bool.TryParse(ConfigurationManager.AppSettings["DeleteIfOK"], out isDelete);
            if ((ConfigurationManager.AppSettings["SpecificDateYear"].ToString().Length > 0) &&
                (ConfigurationManager.AppSettings["SpecificDateMonth"].ToString().Length > 0) &&
                (ConfigurationManager.AppSettings["SpecificDateDay"].ToString().Length > 0))
            {
                int year = 0;
                int month = 0;
                int day = 0;
                if (int.TryParse(ConfigurationManager.AppSettings["SpecificDateYear"], out year) &&
                    int.TryParse(ConfigurationManager.AppSettings["SpecificDateMonth"], out month) &&
                    int.TryParse(ConfigurationManager.AppSettings["SpecificDateDay"], out day))
                {
                    dateTime = new DateTime(year, month, day);
                    LogWriter.WriteToLog(string.Format("Specific Day set, setting 'DaysBack' will be ignored!"));
                    ProcessingDay();
                }
                else
                {
                    throw new Exception("Error: DateTime Parse error!");
                }
            }
            else
            {
                dateTime = DateTime.Now;
                int numDaysBack = 0;
                int.TryParse(ConfigurationManager.AppSettings["DaysBack"], out numDaysBack);
                for (int days = 0; days <= numDaysBack; days++)
                {
                    dateTime = DateTime.Now.AddDays(-days);
                    ProcessingDay();
                }
            }
            LogWriter.WriteToLog(string.Format("Totaltime to process : {0:hh\\:mm\\:ss}", stopwatchTotal.Elapsed));
            stopwatchTotal.Stop();
        }

        private static string convertHourNumberToLetter(string hour)
        {
            switch (hour)
            {
                case "00":
                    return "A";
                case "01":
                    return "B";
                case "02":
                    return "C";
                case "03":
                    return "D";
                case "04":
                    return "E";
                case "05":
                    return "F";
                case "06":
                    return "G";
                case "07":
                    return "H";
                case "08":
                    return "I";
                case "09":
                    return "J";
                case "10":
                    return "K";
                case "11":
                    return "L";
                case "12":
                    return "M";
                case "13":
                    return "N";
                case "14":
                    return "O";
                case "15":
                    return "P";
                case "16":
                    return "Q";
                case "17":
                    return "R";
                case "18":
                    return "S";
                case "19":
                    return "T";
                case "20":
                    return "U";
                case "21":
                    return "V";
                case "22":
                    return "W";
                case "23":
                    return "X";
                default:
                    LogWriter.WriteToLog(string.Format("Waring:{0} convertHourNumberToLetter NOK!", hour));
                    return "Z";
            }
        }

        private static void SyncNPR()
        {
            LogWriter.WriteToLog(string.Format("==> Processing NPR"));
            DirectoryInfo nprInputDIR = new DirectoryInfo(Path.Combine(nprPathTPP01, dateTimePath));
            if (nprInputDIR.Exists)
            {
                FileInfo[] nprInputFiles = nprInputDIR.GetFiles("*.npr");
                foreach (FileInfo sourceNPRFileTPP1 in nprInputFiles)
                {
                    FileInfo sourceNPRFileTPP2 = new FileInfo(Path.Combine(Path.Combine(nprPathTPP02, dateTimePath), sourceNPRFileTPP1.Name));
                    if (sourceNPRFileTPP1.Exists && sourceNPRFileTPP2.Exists)
                    {
                        if (sourceNPRFileTPP1.Length >= sourceNPRFileTPP2.Length)
                        {
                            if (!FilesAreEqual(sourceNPRFileTPP1, new FileInfo(Path.Combine(dateDataShareNPRPath, sourceNPRFileTPP1.Name))))
                            {
                                if (!Path.GetFileNameWithoutExtension(sourceNPRFileTPP1.Name).EndsWith(convertHourNumberToLetter(DateTime.Now.ToString("HH"))))
                                {
                                    sourceNPRFileTPP1.CopyTo(Path.Combine(dateDataShareNPRPath, sourceNPRFileTPP1.Name), true);
                                    LogWriter.WriteToLog(string.Format("Copy NRP:{0} from TPP01.", sourceNPRFileTPP1.Name));
                                }
                            }
                        }
                        else
                        {
                            if (!FilesAreEqual(sourceNPRFileTPP2, new FileInfo(Path.Combine(dateDataShareNPRPath, sourceNPRFileTPP2.Name))))
                            {
                                if (!Path.GetFileNameWithoutExtension(sourceNPRFileTPP1.Name).EndsWith(convertHourNumberToLetter(DateTime.Now.ToString("HH"))))
                                {
                                    sourceNPRFileTPP2.CopyTo(Path.Combine(dateDataShareNPRPath, sourceNPRFileTPP2.Name), true);
                                    LogWriter.WriteToLog(string.Format("Copy NRP:{0} from TPP02.", sourceNPRFileTPP2.Name));
                                }
                            }
                        }
                    }
                    else
                    {
                        if (sourceNPRFileTPP1.Exists)
                        {
                            if (!FilesAreEqual(sourceNPRFileTPP1, new FileInfo(Path.Combine(dateDataShareNPRPath, sourceNPRFileTPP1.Name))))
                            {
                                if (!Path.GetFileNameWithoutExtension(sourceNPRFileTPP1.Name).EndsWith(convertHourNumberToLetter(DateTime.Now.ToString("HH"))))
                                {

                                    sourceNPRFileTPP1.CopyTo(Path.Combine(dateDataShareNPRPath, sourceNPRFileTPP1.Name), true);
                                    LogWriter.WriteToLog(string.Format("Copy NRP:{0} from TPP01.", sourceNPRFileTPP1.Name));
                                }
                            }
                        }
                        else if (sourceNPRFileTPP2.Exists)
                        {
                            if (!FilesAreEqual(sourceNPRFileTPP2, new FileInfo(Path.Combine(dateDataShareNPRPath, sourceNPRFileTPP2.Name))))
                            {
                                if (!Path.GetFileNameWithoutExtension(sourceNPRFileTPP1.Name).EndsWith(convertHourNumberToLetter(DateTime.Now.ToString("HH"))))
                                {
                                    sourceNPRFileTPP2.CopyTo(Path.Combine(dateDataShareNPRPath, sourceNPRFileTPP2.Name), true);
                                    LogWriter.WriteToLog(string.Format("Copy NRP:{0} from TPP02.", sourceNPRFileTPP2.Name));
                                }
                            }
                        }
                        else
                        {
                            LogWriter.WriteToLog(string.Format("Error: no valid NPR file on TPP01 and TPP02  for day: {0}", dateTime.DayOfYear));
                        }
                    }
                }
            }
            else
            {
                LogWriter.WriteToLog(string.Format("Warning: NPR folder does not exist: {0}", nprInputDIR.FullName));
            }
        }

        static bool FilesAreEqual(FileInfo first, FileInfo second)
        {
            if (first.Exists && second.Exists)
            {
                if (first.Length == second.Length)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private static void Cleanup()
        {
            if(isDelete)
            {
                if (Directory.Exists(archivPathTPP01))
                {
                    Directory.Delete(archivPathTPP01, true);
                }
                if (Directory.Exists(archivPathTPP02))
                {
                    Directory.Delete(archivPathTPP02, true);
                }
                if (Directory.Exists(shopPathTPP01))
                {
                    Directory.Delete(shopPathTPP01, true);
                }
                if (Directory.Exists(shopPathTPP02))
                {
                    Directory.Delete(shopPathTPP02, true);
                }
            }
        }
        private static void ProcessingDay()
        {
            dbH = new DBHelper(Path.Combine(dbPath, string.Format("{0}_{1}", DateTime.Now.ToString("yyyy"), dbFile)));
            dateTimePath = string.Format("RefData.{0}\\Month.{1}\\Day.{2:D2}", dateTime.Year % 100, CultureInfo.CreateSpecificCulture("en").DateTimeFormat.GetAbbreviatedMonthName(dateTime.Month), dateTime.Day);
            dateDataShareShopPath = Path.Combine(dataShareShopPath, dateTimePath);
            if (!Directory.Exists(dateDataShareShopPath))
            {
                Directory.CreateDirectory(dateDataShareShopPath);
            }
            dateDataShareNPRPath = Path.Combine(dataShareNPRPath, dateTimePath);
            if (!Directory.Exists(dateDataShareNPRPath))
            {
                Directory.CreateDirectory(dateDataShareNPRPath);
            }
            stopwatchStep.Start();
            LogWriter.WriteToLog(string.Format("=> Processing Day:{0}", dateTime.ToShortDateString()));
            SyncNPR();
            foreach (string archivePathElement in archivePathList)
            {
                LogWriter.WriteToLog(string.Format("==> Processing Element:{0}", archivePathElement));
                DirectoryInfo dateArchivInputDIRTPP01 = new DirectoryInfo(Path.Combine(Path.Combine(archivPathTPP01, archivePathElement), dateTimePath));
                DirectoryInfo dateArchivInputDIRTPP02 = new DirectoryInfo(Path.Combine(Path.Combine(archivPathTPP02, archivePathElement), dateTimePath));
                if (!dateArchivInputDIRTPP01.Exists && !dateArchivInputDIRTPP02.Exists)
                {
                    LogWriter.WriteToLog(string.Format("Warning: Input directorys on TPP01 and TPP02 do not exist:{0} {1}", dateArchivInputDIRTPP01, dateArchivInputDIRTPP02));
                }
                else if(!dateArchivInputDIRTPP01.Exists && dateArchivInputDIRTPP02.Exists)
                {
                    LogWriter.WriteToLog(string.Format("Warning: Input directory only on TPP02 exists!"));
                    SynchronizeOrderTPP02(dateArchivInputDIRTPP02.GetFiles("*.xml"), archivePathElement);
                }
                else if (dateArchivInputDIRTPP01.Exists && !dateArchivInputDIRTPP02.Exists)
                {
                    LogWriter.WriteToLog(string.Format("Warning: Input directory only on TPP01 exists!"));
                    SynchronizeOrderTPP01(dateArchivInputDIRTPP01.GetFiles("*.xml"), archivePathElement);
                }
                else
                {
                    if (!Directory.Exists(Path.Combine(Path.Combine(dataShareArchivePath,archivePathElement),dateTimePath)))
                    {
                        Directory.CreateDirectory(Path.Combine(Path.Combine(dataShareArchivePath, archivePathElement), dateTimePath));
                    }
                    FileInfo[] rinexInputXMLFilesTPP01 = dateArchivInputDIRTPP01.GetFiles("*.xml");
                    FileInfo[] rinexInputXMLFilesTPP02 = dateArchivInputDIRTPP02.GetFiles("*.xml");
                    FileInfo[] restFiles = GetRestListFromSmaller(rinexInputXMLFilesTPP01, rinexInputXMLFilesTPP02);
                    if (rinexInputXMLFilesTPP01.Length >= rinexInputXMLFilesTPP02.Length)
                    {
                        SynchronizeOrderTPP01(rinexInputXMLFilesTPP01, archivePathElement);
                        if (restFiles.Length > 0)
                        {
                            LogWriter.WriteToLog(string.Format("Restfiles to process : {0}", restFiles.Length));
                            SynchronizeOrderTPP02(restFiles, archivePathElement);
                        }
                    }
                    else
                    {
                        SynchronizeOrderTPP02(rinexInputXMLFilesTPP02, archivePathElement);
                        if (restFiles.Length > 0)
                        {
                            LogWriter.WriteToLog(string.Format("Restfiles to process : {0}", restFiles.Length));
                            SynchronizeOrderTPP01(restFiles, archivePathElement);
                        }
                    }
                    LogWriter.WriteToLog(string.Format("Time to process : {0:hh\\:mm\\:ss}", stopwatchStep.Elapsed));
                    stopwatchStep.Restart();
                }
            }
        }

        private static FileInfo[] GetRestListFromSmaller(FileInfo[] rinexInputXMLFilesTPP01, FileInfo[] rinexInputXMLFilesTPP02)
        {
            List<FileInfo> filesTPP01 = new List<FileInfo>();
            List<FileInfo> filesTPP02 = new List<FileInfo>();
            List<FileInfo> restFiles = new List<FileInfo>();
            filesTPP01.AddRange(rinexInputXMLFilesTPP01);
            filesTPP02.AddRange(rinexInputXMLFilesTPP02);
            if (filesTPP01.Count >= filesTPP02.Count)
            {
                bool fileFound = false;              
                foreach (FileInfo file2 in filesTPP02)
                {
                    fileFound = false;
                    foreach (FileInfo file in filesTPP01)
                    {
                        if (file2.Name.Equals(file.Name))
                        {
                            fileFound = true;
                        }
                    }
                    if (!fileFound)
                    {
                        restFiles.Add(file2);
                    }
                }
            }
            else 
            {
                bool fileFound = false;
                foreach (FileInfo file in filesTPP01)
                {
                    fileFound = false;
                    foreach (FileInfo file2 in filesTPP02)
                    {
                        if (file.Name.Equals(file2.Name))
                        {
                            fileFound = true;
                        }
                    }
                    if (!fileFound)
                    {
                        restFiles.Add(file);
                    }
                }
            }
            return restFiles.ToArray();
        }

        private static void SynchronizeOrderTPP01(FileInfo[] rinexInputXMLFilesTPP01, string archivePathElement)
        {
            int countFiles = 0;
            foreach (FileInfo file in rinexInputXMLFilesTPP01)
            {
                XmlMetaDataReader xmlReaderTPP01 = new XmlMetaDataReader(file.FullName, "TPP01");
                int numberOfObservationsTPP01 = xmlReaderTPP01.NumberOfObservations;
                string rinexLongName = xmlReaderTPP01.RinexLongName;
                XmlMetaDataReader xmlReaderTPP02 = new XmlMetaDataReader(file.FullName.Replace(archivPathTPP01, archivPathTPP02), "TPP02");
                int numberOfObservationsTPP02 = xmlReaderTPP02.NumberOfObservations;

                FileInfo sourceXmlFile;
                FileInfo sourceZipFile;
                FileInfo sourceNavFile;
                FileInfo sourceCRXFile;
                if (numberOfObservationsTPP01 >= numberOfObservationsTPP02)
                {
                    sourceXmlFile = file;
                    sourceZipFile = new FileInfo(Path.ChangeExtension(file.FullName, "zip"));
                    sourceNavFile = new FileInfo(Path.Combine(Path.Combine(shopPathTPP01, dateTimePath), string.Format("{0}{1}", rinexLongName.Remove(rinexLongName.Length - 1, 1), "N.rnx")));
                    if (!sourceNavFile.Exists)
                    {
                        string newRinexLongName = rinexLongName.Replace("_01S_", "_");
                        sourceNavFile = new FileInfo(Path.Combine(Path.Combine(shopPathTPP01, dateTimePath), string.Format("{0}{1}", newRinexLongName.Remove(newRinexLongName.Length - 1, 1), "N.rnx")));
                    }
                    sourceCRXFile = new FileInfo(Path.Combine(Path.Combine(shopPathTPP01, dateTimePath), string.Format("{0}{1}", rinexLongName.Remove(rinexLongName.Length - 1, 1), "O.crx")));
                    dbH.loadDBRecord(xmlReaderTPP01.HeaderInfo);
                }
                else
                {
                    sourceXmlFile = new FileInfo(file.FullName.Replace(archivPathTPP01, archivPathTPP02));
                    sourceZipFile = new FileInfo(Path.ChangeExtension(sourceXmlFile.FullName, "zip"));
                    sourceNavFile = new FileInfo(Path.Combine(Path.Combine(shopPathTPP02, dateTimePath), string.Format("{0}{1}", rinexLongName.Remove(rinexLongName.Length - 1, 1), "N.rnx")));
                    if (!sourceNavFile.Exists)
                    {
                        string newRinexLongName = rinexLongName.Replace("_01S_", "_");
                        sourceNavFile = new FileInfo(Path.Combine(Path.Combine(shopPathTPP02, dateTimePath), string.Format("{0}{1}", newRinexLongName.Remove(newRinexLongName.Length - 1, 1), "N.rnx")));
                    }
                    sourceCRXFile = new FileInfo(Path.Combine(Path.Combine(shopPathTPP02, dateTimePath), string.Format("{0}{1}", rinexLongName.Remove(rinexLongName.Length - 1, 1), "O.crx")));
                    dbH.loadDBRecord(xmlReaderTPP02.HeaderInfo);
                }
                string xmlDataSharePath = Path.Combine(Path.Combine(Path.Combine(dataShareArchivePath, archivePathElement), dateTimePath), sourceXmlFile.Name);
                string zipDataSharePath = Path.Combine(Path.Combine(Path.Combine(dataShareArchivePath, archivePathElement), dateTimePath), sourceZipFile.Name);
                string navDataSharePath = Path.Combine(dateDataShareShopPath, sourceNavFile.Name);
                string crxDataSharePath = Path.Combine(dateDataShareShopPath, sourceCRXFile.Name);
                if (!sourceXmlFile.Exists || !sourceZipFile.Exists)
                {
                    LogWriter.WriteToLog(string.Format("Warning: Sourcefile for base {0} does not exist!", file.FullName));
                }
                else
                {
                    sourceXmlFile.CopyTo(xmlDataSharePath, true);
                    sourceZipFile.CopyTo(zipDataSharePath, true);
                    if (archivePathElement.Equals("Shop") && sourceNavFile.Exists && sourceCRXFile.Exists)
                    {
                        sourceNavFile.CopyTo(navDataSharePath, true);
                        sourceCRXFile.CopyTo(crxDataSharePath, true);
                    }
                    else
                    {
                        LogWriter.WriteToLog(string.Format("Warning: Sourcefiles {0} or {1} do not exists!! Can't copy files.", sourceNavFile.FullName, sourceCRXFile.FullName));
                    }
                    countFiles++;
                }
            }
            LogWriter.WriteToLog(string.Format("Total {0} synchronizations processed.", countFiles));
        }

        private static void SynchronizeOrderTPP02(FileInfo[] rinexInputXMLFilesTPP02, string archivePathElement)
        {
            int countFiles = 0;
            foreach (FileInfo file in rinexInputXMLFilesTPP02)
            {
                XmlMetaDataReader xmlReaderTPP02 = new XmlMetaDataReader(file.FullName, "TPP02");
                int numberOfObservationsTPP02 = xmlReaderTPP02.NumberOfObservations;
                string rinexLongName = xmlReaderTPP02.RinexLongName;
                XmlMetaDataReader xmlReaderTPP01 = new XmlMetaDataReader(file.FullName.Replace(archivPathTPP02, archivPathTPP01), "TPP01");
                int numberOfObservationsTPP01 = xmlReaderTPP01.NumberOfObservations;

                FileInfo sourceXmlFile;
                FileInfo sourceZipFile;
                FileInfo sourceRnxFile;
                FileInfo sourceCRXFile;
                if (numberOfObservationsTPP02 >= numberOfObservationsTPP01)
                {
                    sourceXmlFile = file;
                    sourceZipFile = new FileInfo(Path.ChangeExtension(file.FullName, "zip"));
                    sourceRnxFile = new FileInfo(Path.Combine(Path.Combine(shopPathTPP02, dateTimePath), string.Format("{0}{1}", rinexLongName.Remove(rinexLongName.Length - 1, 1), "N.rnx")));
                    sourceCRXFile = new FileInfo(Path.Combine(Path.Combine(shopPathTPP02, dateTimePath), string.Format("{0}{1}", rinexLongName.Remove(rinexLongName.Length - 1, 1), "O.crx")));
                    dbH.loadDBRecord(xmlReaderTPP02.HeaderInfo);
                }
                else
                {
                    sourceXmlFile = new FileInfo(file.FullName.Replace(archivPathTPP02, archivPathTPP01));
                    sourceZipFile = new FileInfo(Path.ChangeExtension(sourceXmlFile.FullName, "zip"));
                    sourceRnxFile = new FileInfo(Path.Combine(Path.Combine(shopPathTPP01, dateTimePath), string.Format("{0}{1}", rinexLongName.Remove(rinexLongName.Length - 1, 1), "N.rnx")));
                    sourceCRXFile = new FileInfo(Path.Combine(Path.Combine(shopPathTPP01, dateTimePath), string.Format("{0}{1}", rinexLongName.Remove(rinexLongName.Length - 1, 1), "O.crx")));
                    dbH.loadDBRecord(xmlReaderTPP01.HeaderInfo);
                }
                string xmlDataSharePath = Path.Combine(Path.Combine(Path.Combine(dataShareArchivePath, archivePathElement), dateTimePath), sourceXmlFile.Name);
                string zipDataSharePath = Path.Combine(Path.Combine(Path.Combine(dataShareArchivePath, archivePathElement), dateTimePath), sourceZipFile.Name);
                string rnxDataSharePath = Path.Combine(dateDataShareShopPath, sourceRnxFile.Name);
                string crxDataSharePath = Path.Combine(dateDataShareShopPath, sourceCRXFile.Name);
                if (!sourceXmlFile.Exists || !sourceZipFile.Exists)
                {
                    LogWriter.WriteToLog(string.Format("Warning: Sourcefile for base {0} does not exist!", file.FullName));
                }
                else
                {
                    sourceXmlFile.CopyTo(xmlDataSharePath, true);
                    sourceZipFile.CopyTo(zipDataSharePath, true);
                    if (archivePathElement.Equals("Shop") && sourceRnxFile.Exists && sourceCRXFile.Exists)
                    {
                        sourceRnxFile.CopyTo(rnxDataSharePath, true);
                        sourceCRXFile.CopyTo(crxDataSharePath, true);
                    }
                    countFiles++;
                }
            }
            LogWriter.WriteToLog(string.Format("Total {0} synchronizations processed.", countFiles));
        }
    }
}
