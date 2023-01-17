# service-rinexsynchronizer

Synchronizes NPR and rinex data from 2 sources to one destination

## Summary

The service is designed to synchronize NPR and rinex data from 2 sources to one destination.
It decides which is the more complete file based on the rinex metadata (service-rinexmetadatacontroller).
All configuration parameters are listed in the configuration file App.config.

## Dependencies

Prerequisites for development:
  * c# .Net Framework 4.5
  * System.Data.SQLite.Core -Version 1.0.111 (The official SQLite database engine for both x86 and x64 along with the ADO.NET provider.)
  
To get the System.Data.SQLite.Core Package use nuget.
PM> Install-Package System.Data.SQLite.Core -Version 1.0.111

## Flowchart
### Processing a single day of a single input directory

```mermaid
flowchart TD
  Start --> CheckPrevious{Did previous processes run? using logfiles of previous procs, max 10 minutes}
  CheckPrevious -- No --> CheckPrevious
  CheckPrevious -- Yes --> CreateDayFolder[Create daily target folder in data shop]
  CreateDayFolder --> CreateNPRFolder[Create daily NPR target folder]
  CreateNPRFolder --> SyncNPR[Sync multiple NPR inputs directories to target NPR folder]
  SyncNPR --> ForEveryInput{"For every input folder type (DAFI, Shop, NoShop)"}
  ForEveryInput --> CheckSources["Choose Input Archive with most *.xml files as start (TPP01, TPP02)"]
  CheckSources --> ProcessDirectory[Process input Archive Directory]
  ProcessDirectory --> NextXML{For each XML File in Input directory}
  NextXML --> CheckObservations["Choose from TPP01/TPP02 file with more observations"]
  CheckObservations --> CreateSourceNames[Create names of source files based on rinex longname in XML, zip, rnx, crc]
  CreateSourceNames --> WriteDB[Write XML information of chosen file to DB]
  WriteDB --> CopyArchive[Copy chosen file to Archive Path]
  CopyArchive --> CopyShop[OPTIONAL: copy chosen file to Shop]
  CopyShop -- MoreFiles --> NextXML
  NextXML -- NoMoreFiles --> HasMore{Has more input directories?}
  HasMore -- Yes --> ProcessDirectory
  HasMore -- No --> HasMoreInput{Has more input types?}
  HasMoreInput -- Yes --> ForEveryInput
  HasMoreInput -- No --> End[End]

 
```



