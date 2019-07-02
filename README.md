# adifpush
.NET Core console app to push wsjtx_log.adi into Cloudlog 

Right now this just pushes that file one-off. In the near future it will watch wsjtx_log.adi for changes and auto-upload live, including while QSOs are ongoing, removing the need to log manually.

# Prerequisites
- .NET Core 2.1 SDK and/or runtime, not sure ([download page](https://dotnet.microsoft.com/download/dotnet-core/2.1))
- Any OS that .NET Core 2.1 runs on. Built on OS X, should work fine on Windows and Linux too.
- Recent Cloudlog install + API key for it (from the Admin menu)

# Usage
```
git clone https://github.com/M0LTE/adifpush.git
cd adifpush/adifpush
dotnet run --configure
```
and follow the steps. Supply your cloudlog base URL, and a read/write API key.

Then, for one-off upload: (dupe checking is server side)
```
cd adifpush/adifpush
dotnet run /path/to/wsjtx_log.adi
```
