# Database & log4net Structured Logging Guide

UnoWebTemplate uses **log4net** wrapped under ASP.NET Core logging providers (`Microsoft.Extensions.Logging.Log4Net.AspNetCore`) to route logs to multiple outputs.

---

## 🪵 Log Appenders Configuration

Inside [log4net.config](file:///home/jaret/Documents/GitHub/UnoWebTemplate/UnoWebTemplate.Server/log4net.config), four separate log writers (Appenders) are defined:

1. **ConsoleAppender**: Formats logs as `date [thread] level logger - message` and writes to standard output (`stdout`), formatted for docker logs and console.
2. **DebugAppender**: Sends active messages to the IDE debug output console.
3. **RollingFileAppender**: Writes logs to `logs/app.log` (created within the assembly execution directory). Files roll daily and are capped at `10MB` with up to 5 backups.
4. **AdoNetAppender**: Connects to the database and logs entries directly to the `Logs` database table.

---

## 🗄️ Database SQLite Logging

To support zero-configuration local runs, the backend uses **SQLite** as its default database engine.

### Database Setup
At startup, `Program.cs` calls `db.Database.EnsureCreated();` to check if `app.db` exists. If not, it creates the database file and creates the log table based on the schema mapping in `AppDbContext`:

```csharp
public class LogEntry
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string? Thread { get; set; }
    public string? Level { get; set; }
    public string? Logger { get; set; }
    public string? Message { get; set; }
    public string? Exception { get; set; }
}
```

### Dynamic Connection String Mapping
Because log4net is configured via an XML file, connection strings are typically hardcoded. To prevent this, `Program.cs` intercepts the log4net hierarchy at startup and overrides the connection string of the database appender to match EF Core:

```csharp
var hierarchy = (Hierarchy)LogManager.GetRepository(entryAssembly);
var adoNetAppenders = hierarchy.GetAppenders().OfType<AdoNetAppender>();
foreach (var appender in adoNetAppenders)
{
    appender.ConnectionString = connectionString;
    appender.ActivateOptions(); // Reinitializes the database connection
}
```

---

## 🚨 Deadlock Loop Prevention

A typical issue in database logging occurs when database transactions generate log messages. If Entity Framework Core logs SQL queries, and log4net routes those logs to the database using SQL inserts, it will create a **recursive circular loop** that locks the database and deadlocks the thread.

To prevent this, `log4net.config` declares specific overrides for `Microsoft` and `System` namespaces. By setting `additivity="false"`, system and database logs are kept out of the `AdoNetAppender`, while still being routed safely to the Console, Debug, and File outputs:

```xml
<logger name="Microsoft" additivity="false">
  <level value="WARN" />
  <appender-ref ref="ConsoleAppender" />
  <appender-ref ref="DebugAppender" />
  <appender-ref ref="RollingFileAppender" />
</logger>

<logger name="System" additivity="false">
  <level value="WARN" />
  <appender-ref ref="ConsoleAppender" />
  <appender-ref ref="DebugAppender" />
  <appender-ref ref="RollingFileAppender" />
</logger>
```
