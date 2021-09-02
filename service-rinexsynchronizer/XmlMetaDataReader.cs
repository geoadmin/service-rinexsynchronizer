using System.Xml;
using System.IO;

namespace RinexSynchronizer
{
    class XmlMetaDataReader
    {

        private string rinexVersion;
        private string rinexShortName;
        private string rinexLongName;
        private string name;
        private double x;
        private double y;
        private double z;
        private double intervall;
        private string firstObservation;
        private string lastObservation;
        private int numberOfObservations;
        private int numberOfSatellites;
        private string observer;
        private string observerType;
        private string observerNr;
        private string observerFirmware;
        private string gnssTimeSystem;
        private string antennaType;
        private string antennaNr;
        private double antennaDeltaE;
        private double antennaDeltaN;
        private double antennaDeltaH;
        private string agency;
        private string satelliteSystem;
        private string creatorInfo;
        private string creatorName;
        private string creationDateTime;
        private int gpsObservationTypes;
        private int gloObservationTypes;
        private int galObservationTypes;
        private int bdsObservationTypes;
        private string checksum;
        private string checksumMD5;
        private bool isShop;
        private string xmlFilename;
        private HeaderInfo headerInfo;


        public XmlMetaDataReader(string xmlFilename,string recordingSystem)
        {
            this.xmlFilename = xmlFilename;
            ReadXML();
            headerInfo = new HeaderInfo(rinexVersion,rinexShortName,rinexLongName,name,x,y,z,intervall,firstObservation,lastObservation,numberOfObservations,numberOfSatellites,
            observer,observerType,observerNr,observerFirmware,gnssTimeSystem,antennaType,antennaNr,antennaDeltaE,antennaDeltaN,
            antennaDeltaH,agency,satelliteSystem,creatorInfo,creatorName,creationDateTime,gpsObservationTypes,gloObservationTypes,galObservationTypes,bdsObservationTypes,checksum,checksumMD5,isShop, recordingSystem);
        }

        private void ReadXML()
        {
            if (File.Exists(xmlFilename))
            {
                using (XmlReader reader = XmlReader.Create(xmlFilename))
                {
                    while (reader.Read())
                    {
                        switch (reader.Name)
                        {
                            case "RinexVersion":
                                rinexVersion = reader.ReadElementContentAsString();
                                break;
                            case "RinexShortName":
                                rinexShortName = reader.ReadElementContentAsString();
                                break;
                            case "RinexLongName":
                                rinexLongName = reader.ReadElementContentAsString();
                                break;
                            case "Name":
                                name = reader.ReadElementContentAsString();
                                break;
                            case "X":
                                x = reader.ReadElementContentAsDouble();
                                break;
                            case "Y":
                                y = reader.ReadElementContentAsDouble();
                                break;
                            case "Z":
                                z = reader.ReadElementContentAsDouble();
                                break;
                            case "Intervall":
                                double.TryParse(reader.GetAttribute("value"),out intervall); 
                                break;
                            case "FirstObservation":
                                firstObservation = reader.ReadElementContentAsString();
                                break;
                            case "LastObservation":
                                lastObservation = reader.ReadElementContentAsString();
                                break;
                            case "NumberOfObservations":
                                numberOfObservations = reader.ReadElementContentAsInt();
                                break;
                            case "NumberOfSatellites":
                                numberOfSatellites = reader.ReadElementContentAsInt();
                                break;
                            case "Observer":
                                observer = reader.ReadElementContentAsString();
                                break;
                            case "ObserverType":
                                observerType = reader.ReadElementContentAsString();
                                break;
                            case "ObserverNr":
                                observerNr = reader.ReadElementContentAsString();
                                break;
                            case "ObserverFirmware":
                                observerFirmware = reader.ReadElementContentAsString();
                                break;
                            case "GnssTimeSystem":
                                gnssTimeSystem = reader.ReadElementContentAsString();
                                break;
                            case "AntennaType":
                                antennaType = reader.ReadElementContentAsString();
                                break;
                            case "AntennaNr":
                                antennaNr = reader.ReadElementContentAsString();
                                break;
                            case "AntennaDeltaE":
                                antennaDeltaE = reader.ReadElementContentAsDouble();
                                break;
                            case "AntennaDeltaN":
                                antennaDeltaN = reader.ReadElementContentAsDouble();
                                break;
                            case "AntennaDeltaH":
                                antennaDeltaH = reader.ReadElementContentAsDouble();
                                break;
                            case "Agency":
                                agency = reader.ReadElementContentAsString();
                                break;
                            case "SatelliteSystem":
                                satelliteSystem = reader.ReadElementContentAsString();
                                break;
                            case "CreatorInfo":
                                creatorInfo = reader.ReadElementContentAsString();
                                break;
                            case "CreatorName":
                                creatorName = reader.ReadElementContentAsString();
                                break;
                            case "CreationDateTime":
                                creationDateTime = reader.ReadElementContentAsString();
                                break;
                            case "GpsObservationTypes":
                                gpsObservationTypes = reader.ReadElementContentAsInt();
                                break;
                            case "GloObservationTypes":
                                gloObservationTypes = reader.ReadElementContentAsInt();
                                break;
                            case "GalObservationTypes":
                                galObservationTypes = reader.ReadElementContentAsInt();
                                break;
                            case "BdsObservationTypes":
                                bdsObservationTypes = reader.ReadElementContentAsInt();
                                break;
                            case "Checksum":
                                if (reader.GetAttribute("Algorithmus").Equals("MD5"))
                                {
                                    checksumMD5 = reader.GetAttribute("value");
                                }
                                else
                                {
                                    checksum = reader.GetAttribute("value");
                                }
                                break;
                            case "IsShop":
                                bool.TryParse(reader.ReadElementContentAsString(), out isShop);
                                break;
                        }
                    }
                }
            }
            else
            {
                rinexLongName = "";
                numberOfObservations = 0;
            }
        }

        public HeaderInfo HeaderInfo
        {
            get
            {
                return headerInfo;
            }
        }

        public string RinexLongName
        {
            get
            {
                return rinexLongName;
            }

            set
            {
                rinexLongName = value;
            }
        }

        
        public int NumberOfObservations
        {
            get
            {
                return numberOfObservations;
            }

            set
            {
                numberOfObservations = value;
            }
        }
    }
}
