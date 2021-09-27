# adifpush
.NET Core console app to push wsjtx_log.adi into Cloudlog 

Listens for the UDP interface of WSJT-X and JTDX and auto-uploads new contacts live, including while QSOs are ongoing, removing the need to log manually.

(Still need to have "Prompt me to log QSO" turned on in WSJT-X, and the network interface turned on)

# Prerequisites
- .NET Core 3.1 SDK ([download page](https://dotnet.microsoft.com/download))
- Any OS that .NET runs on. Built on OS X, should work fine on Windows and Linux too.
- Recent Cloudlog install + API key for it (from the Admin menu)

# Usage
## One-off setup
```
git clone https://github.com/M0LTE/adifpush.git
cd adifpush/adifpush
dotnet run --configure
```
and follow the steps. Supply your cloudlog base URL, and a read/write API key.

## Day-to-day
For auto-upload of WSJT-X's logfile, simply start with no command line parameters:
```
cd adifpush/adifpush
dotnet run
```

or for one-off upload of the whole log, e.g. for missed contacts: (dupe checking is server side)
```
cd adifpush/adifpush
dotnet run /path/to/wsjtx_log.adi
```
