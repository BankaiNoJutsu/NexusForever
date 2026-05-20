using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using NexusForever.Cryptography;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Network.Auth.Static;
using NexusForever.Network.Message;
using PlayerClass = NexusForever.Game.Static.Entity.Class;
using PlayerPath = NexusForever.Game.Static.PlayerPath.Path;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
            return Usage();

        try
        {
            string command = args[0].ToLowerInvariant();
            return command switch
            {
                "seed" => await SeedCommand.Run(new CliArgs(args.Skip(1))),
                "run" when args.Length > 1 && args[1].Equals("login-world", StringComparison.OrdinalIgnoreCase)
                    => await LoginWorldCommand.Run(new CliArgs(args.Skip(2))),
                "report" => await ReportCommand.Run(new CliArgs(args.Skip(1))),
                _ => Usage()
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  NexusForever.LoadTest seed --profile typical --users 1");
        Console.Error.WriteLine("  NexusForever.LoadTest run login-world --user loadtest0001@example.local --password loadtest --samples 30 --warmup 3");
        Console.Error.WriteLine("  NexusForever.LoadTest run login-world --profile typical --concurrent-users 50 --duration 5m");
        Console.Error.WriteLine("  NexusForever.LoadTest report --input artifacts\\load-tests");
        return 1;
    }
}

internal sealed class CliArgs
{
    private readonly Dictionary<string, string?> values = new(StringComparer.OrdinalIgnoreCase);

    public CliArgs(IEnumerable<string> args)
    {
        string? pending = null;
        foreach (string arg in args)
        {
            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                if (pending != null)
                    values[pending] = "true";

                pending = arg[2..];
                continue;
            }

            if (pending == null)
                continue;

            values[pending] = arg;
            pending = null;
        }

        if (pending != null)
            values[pending] = "true";
    }

    public string GetString(string name, string fallback)
    {
        return values.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    public int GetInt(string name, int fallback)
    {
        return values.TryGetValue(name, out string? value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : fallback;
    }

    public bool Has(string name)
    {
        return values.ContainsKey(name);
    }
}

internal static class Defaults
{
    public const string AuthConnectionString = "server=127.0.0.1;port=3306;user=nexusforever;password=nexusforever;database=nexus_forever_auth";
    public const string CharacterConnectionString = "server=127.0.0.1;port=3306;user=nexusforever;password=nexusforever;database=nexus_forever_character";
    public const string Password = "loadtest";
}

internal static class SeedCommand
{
    public static async Task<int> Run(CliArgs args)
    {
        string profile = args.GetString("profile", "typical").ToLowerInvariant();
        int users = args.GetInt("users", 1);
        int startIndex = args.GetInt("start-index", 1);
        string password = args.GetString("password", Defaults.Password);

        await using var auth = new AuthContext(Connection(args.GetString("auth-connection", Defaults.AuthConnectionString)));
        await using var character = new CharacterContext(Connection(args.GetString("character-connection", Defaults.CharacterConnectionString)));

        ulong nextCharacterId = await character.Character.Select(c => c.Id).DefaultIfEmpty().MaxAsync() + 1ul;
        for (int offset = 0; offset < users; offset++)
        {
            int index = startIndex + offset;
            string email = UserEmail(index);
            AccountModel? account = await auth.Account.Include(a => a.AccountRole).SingleOrDefaultAsync(a => a.Email == email);
            if (account == null)
            {
                (string salt, string verifier) = PasswordProvider.GenerateSaltAndVerifier(email, password);
                account = new AccountModel
                {
                    Email      = email,
                    S          = salt,
                    V          = verifier,
                    CreateTime = DateTime.UtcNow
                };

                auth.Account.Add(account);
                await auth.SaveChangesAsync();

                auth.AccountRole.Add(new AccountRoleModel
                {
                    Id     = account.Id,
                    RoleId = 1u
                });
                await auth.SaveChangesAsync();
            }
            else if (!account.AccountRole.Any(r => r.RoleId == 1u))
            {
                auth.AccountRole.Add(new AccountRoleModel
                {
                    Id     = account.Id,
                    RoleId = 1u
                });
                await auth.SaveChangesAsync();
            }

            int characterCount = profile switch
            {
                "minimal" => 1,
                "heavy"   => 6,
                _         => 2
            };

            List<CharacterModel> existing = await character.Character
                .Where(c => c.AccountId == account.Id && c.Name.StartsWith($"Load{index:D4}"))
                .ToListAsync();

            for (int characterIndex = existing.Count; characterIndex < characterCount; characterIndex++)
            {
                CharacterModel model = CreateCharacter(account.Id, nextCharacterId++, index, characterIndex, profile);
                character.Character.Add(model);
            }

            await character.SaveChangesAsync();
            Console.WriteLine($"Seeded {email} with {characterCount} {profile} character(s).");
        }

        return 0;
    }

    private static CharacterModel CreateCharacter(uint accountId, ulong id, int userIndex, int characterIndex, string profile)
    {
        byte level = profile == "heavy" ? (byte)50 : (byte)1;
        var model = new CharacterModel
        {
            Id                     = id,
            AccountId              = accountId,
            Name                   = $"Load{userIndex:D4}{(char)('A' + characterIndex)}",
            Sex                    = (byte)(characterIndex % 2 == 0 ? Sex.Male : Sex.Female),
            Race                   = (byte)(characterIndex % 2 == 0 ? Race.Human : Race.Aurin),
            Class                  = (byte)(characterIndex % 2 == 0 ? PlayerClass.Warrior : PlayerClass.Esper),
            Level                  = level,
            FactionId              = 166,
            CreateTime             = DateTime.UtcNow,
            LastOnline             = DateTime.UtcNow,
            LocationX              = 0f,
            LocationY              = 0f,
            LocationZ              = 0f,
            RotationX              = 0f,
            RotationY              = 0f,
            RotationZ              = 0f,
            WorldId                = 3460,
            WorldZoneId            = 0,
            ActivePath             = (uint)PlayerPath.Soldier,
            PathActivatedTimestamp = DateTime.UtcNow,
            ActiveCostumeIndex     = -1,
            InputKeySet            = 0,
            ActiveSpec             = 0,
            InnateIndex            = 0,
            TotalXp                = level == 50 ? 7934799u : 0u
        };

        foreach (PlayerPath path in Enum.GetValues<PlayerPath>())
        {
            model.Path.Add(new CharacterPathModel
            {
                Id       = id,
                Path     = (byte)path,
                Unlocked = (byte)(path == PlayerPath.Soldier ? 1 : 0)
            });
        }

        model.Stat.Add(new CharacterStatModel { Id = id, Stat = (byte)Stat.Health, Value = level == 50 ? 8000 : 800 });
        model.Stat.Add(new CharacterStatModel { Id = id, Stat = (byte)Stat.Dash, Value = 200 });
        model.Stat.Add(new CharacterStatModel { Id = id, Stat = (byte)Stat.Level, Value = level });
        model.Stat.Add(new CharacterStatModel { Id = id, Stat = (byte)Stat.StandState, Value = 3 });
        model.Stat.Add(new CharacterStatModel { Id = id, Stat = (byte)Stat.Sheathed, Value = 1 });

        if (profile == "heavy")
        {
            for (byte stat = 17; stat < 27; stat++)
                model.Stat.Add(new CharacterStatModel { Id = id, Stat = stat, Value = stat * 10 });
        }

        return model;
    }

    private static DatabaseConnectionString Connection(string connectionString)
    {
        return new DatabaseConnectionString
        {
            Provider = DatabaseProvider.MySql,
            ConnectionString = connectionString
        };
    }

    public static string UserEmail(int index)
    {
        return $"loadtest{index:D4}@example.local";
    }
}

internal static class LoginWorldCommand
{
    public static async Task<int> Run(CliArgs args)
    {
        string outputDirectory = args.GetString("output", Path.Combine("artifacts", "load-tests"));
        Directory.CreateDirectory(outputDirectory);

        var options = new LoginWorldOptions
        {
            User = args.GetString("user", SeedCommand.UserEmail(args.GetInt("start-index", 1))),
            Password = args.GetString("password", Defaults.Password),
            StsHost = args.GetString("sts-host", "127.0.0.1"),
            AuthHost = args.GetString("auth-host", "127.0.0.1"),
            WorldHost = args.GetString("world-host", "127.0.0.1"),
            StsPort = args.GetInt("sts-port", 6600),
            AuthPort = args.GetInt("auth-port", 23115),
            WorldPort = args.GetInt("world-port", 24000),
            Samples = args.GetInt("samples", 1),
            Warmup = args.GetInt("warmup", 0),
            ConcurrentUsers = args.GetInt("concurrent-users", 1),
            Duration = ParseDuration(args.GetString("duration", "")),
            Ramp = args.GetString("ramp", ""),
            AuthConnectionString = args.GetString("auth-connection", Defaults.AuthConnectionString),
            CharacterConnectionString = args.GetString("character-connection", Defaults.CharacterConnectionString)
        };

        var report = new LoadTestRunReport
        {
            Scenario = "login-world",
            StartedUtc = DateTime.UtcNow,
            Options = options.ToDictionary()
        };

        if (!string.IsNullOrWhiteSpace(options.Ramp))
        {
            foreach (int users in options.Ramp.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(int.Parse))
                await RunConcurrentStage(options with { ConcurrentUsers = users, Duration = options.Duration ?? TimeSpan.FromMinutes(5) }, report, $"ramp-{users}");
        }
        else if (options.ConcurrentUsers > 1 || options.Duration.HasValue)
        {
            await RunConcurrentStage(options, report, $"concurrent-{options.ConcurrentUsers}");
        }
        else
        {
            for (int i = 0; i < options.Warmup + options.Samples; i++)
            {
                LoginWorldSample sample = await RunOne(options, options.User, i >= options.Warmup ? "sample" : "warmup");
                if (i >= options.Warmup)
                    report.Samples.Add(sample);
                Console.WriteLine($"{sample.Label} {i + 1}: {(sample.Success ? "ok" : "failed")} {sample.TotalMs:F1} ms");
            }
        }

        report.CompletedUtc = DateTime.UtcNow;
        report.Summary = ReportCommand.Summarise(report.Samples);

        string path = Path.Combine(outputDirectory, $"login-world-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report, JsonOptions));
        Console.WriteLine($"Wrote {path}");
        Console.WriteLine(report.Summary.ToMarkdown());
        return report.Samples.Any(s => !s.Success) ? 2 : 0;
    }

    private static async Task RunConcurrentStage(LoginWorldOptions options, LoadTestRunReport report, string label)
    {
        TimeSpan duration = options.Duration ?? TimeSpan.FromMinutes(1);
        DateTime deadline = DateTime.UtcNow.Add(duration);
        int iteration = 0;

        while (DateTime.UtcNow < deadline)
        {
            List<Task<LoginWorldSample>> tasks = new();
            for (int i = 0; i < options.ConcurrentUsers; i++)
            {
                string user = SeedCommand.UserEmail(i + 1);
                tasks.Add(RunOne(options, user, label));
            }

            LoginWorldSample[] samples = await Task.WhenAll(tasks);
            report.Samples.AddRange(samples);
            iteration++;
            Console.WriteLine($"{label} iteration {iteration}: {samples.Count(s => s.Success)}/{samples.Length} ok");
        }
    }

    private static async Task<LoginWorldSample> RunOne(LoginWorldOptions options, string user, string label)
    {
        var sample = new LoginWorldSample
        {
            User = user,
            Label = label,
            StartedUtc = DateTime.UtcNow
        };

        long totalStart = Stopwatch.GetTimestamp();
        try
        {
            AccountCharacter accountCharacter = await Step(sample, "lookup-mock-data", () => LookupAccountCharacter(options, user));

            string token = await Step(sample, "sts-login-token", async () =>
            {
                await using var sts = new StsProtocolClient(options.StsHost, options.StsPort);
                await sts.ConnectAsync();
                return await sts.LoginAndRequestGameToken(user, options.Password);
            });

            RealmTicket ticket = await Step(sample, "auth-realm-ticket", async () =>
            {
                await using var auth = new GameProtocolClient(options.AuthHost, options.AuthPort, PacketCrypt.GetKeyFromAuthBuildAndMessage());
                await auth.ConnectAsync();
                await auth.WaitForOuterOpcode(GameMessageOpcode.ServerHello, TimeSpan.FromSeconds(10));
                await auth.SendEncrypted(GameMessageOpcode.ClientHelloAuth, writer => PacketWriters.WriteClientHelloAuth(writer, user, token));
                return await auth.WaitForRealmTicket(TimeSpan.FromSeconds(10));
            });

            await Step(sample, "world-connect-character-list", async () =>
            {
                await using var world = new GameProtocolClient(options.WorldHost, options.WorldPort, PacketCrypt.GetKeyFromAuthBuildAndMessage());
                await world.ConnectAsync();
                await world.WaitForInnerOpcode(GameMessageOpcode.ServerHello, TimeSpan.FromSeconds(10));

                await world.SendEncrypted(GameMessageOpcode.ClientHelloRealm,
                    writer => PacketWriters.WriteClientHelloRealm(writer, ticket.AccountId, ticket.SessionKey, user));
                world.SetCrypt(PacketCrypt.GetKeyFromTicket(ticket.SessionKey));

                await world.SendEncrypted(GameMessageOpcode.ClientCharacterList, _ => { });
                await world.WaitForInnerOpcode(GameMessageOpcode.ServerCharacterList, TimeSpan.FromSeconds(15));

                await world.SendEncrypted(GameMessageOpcode.ClientCharacterSelect,
                    writer => writer.Write(accountCharacter.CharacterId));

                await Task.Delay(250);
                await world.SendEncrypted(GameMessageOpcode.ClientEnteredWorld,
                    writer => writer.Write((ushort)accountCharacter.WorldZoneId, 15u));
                await world.WaitForInnerOpcode(GameMessageOpcode.ServerPlayerEnteredWorld, TimeSpan.FromSeconds(15));
            });

            sample.Success = true;
        }
        catch (Exception ex)
        {
            sample.Success = false;
            sample.Error = ex.Message;
        }
        finally
        {
            sample.TotalMs = Stopwatch.GetElapsedTime(totalStart).TotalMilliseconds;
            sample.CompletedUtc = DateTime.UtcNow;
        }

        return sample;
    }

    private static async Task<T> Step<T>(LoginWorldSample sample, string name, Func<Task<T>> action)
    {
        long start = Stopwatch.GetTimestamp();
        try
        {
            return await action();
        }
        finally
        {
            sample.Steps.Add(new LoadTestStep
            {
                Name = name,
                DurationMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds
            });
        }
    }

    private static async Task Step(LoginWorldSample sample, string name, Func<Task> action)
    {
        long start = Stopwatch.GetTimestamp();
        try
        {
            await action();
        }
        finally
        {
            sample.Steps.Add(new LoadTestStep
            {
                Name = name,
                DurationMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds
            });
        }
    }

    private static async Task<AccountCharacter> LookupAccountCharacter(LoginWorldOptions options, string user)
    {
        await using var auth = new AuthContext(new DatabaseConnectionString
        {
            Provider = DatabaseProvider.MySql,
            ConnectionString = options.AuthConnectionString
        });
        await using var character = new CharacterContext(new DatabaseConnectionString
        {
            Provider = DatabaseProvider.MySql,
            ConnectionString = options.CharacterConnectionString
        });

        AccountModel account = await auth.Account.SingleAsync(a => a.Email == user);
        CharacterModel selected = await character.Character
            .Where(c => c.AccountId == account.Id && c.DeleteTime == null)
            .OrderBy(c => c.Id)
            .FirstAsync();

        return new AccountCharacter(account.Id, selected.Id, selected.WorldZoneId);
    }

    private static TimeSpan? ParseDuration(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        char suffix = value[^1];
        if (!double.TryParse(value[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out double amount))
            return TimeSpan.Parse(value, CultureInfo.InvariantCulture);

        return suffix switch
        {
            's' => TimeSpan.FromSeconds(amount),
            'm' => TimeSpan.FromMinutes(amount),
            'h' => TimeSpan.FromHours(amount),
            _   => TimeSpan.Parse(value, CultureInfo.InvariantCulture)
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };
}

internal sealed record LoginWorldOptions
{
    public string User { get; init; } = "";
    public string Password { get; init; } = "";
    public string StsHost { get; init; } = "";
    public string AuthHost { get; init; } = "";
    public string WorldHost { get; init; } = "";
    public int StsPort { get; init; }
    public int AuthPort { get; init; }
    public int WorldPort { get; init; }
    public int Samples { get; init; }
    public int Warmup { get; init; }
    public int ConcurrentUsers { get; init; }
    public TimeSpan? Duration { get; init; }
    public string Ramp { get; init; } = "";
    public string AuthConnectionString { get; init; } = "";
    public string CharacterConnectionString { get; init; } = "";

    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            ["user"] = User,
            ["sts"] = $"{StsHost}:{StsPort}",
            ["auth"] = $"{AuthHost}:{AuthPort}",
            ["world"] = $"{WorldHost}:{WorldPort}",
            ["samples"] = Samples.ToString(CultureInfo.InvariantCulture),
            ["warmup"] = Warmup.ToString(CultureInfo.InvariantCulture),
            ["concurrentUsers"] = ConcurrentUsers.ToString(CultureInfo.InvariantCulture),
            ["duration"] = Duration?.ToString() ?? "",
            ["ramp"] = Ramp
        };
    }
}

internal sealed record AccountCharacter(uint AccountId, ulong CharacterId, ushort WorldZoneId);
internal sealed record RealmTicket(uint AccountId, byte[] SessionKey, uint Address, ushort Port);

internal sealed class StsProtocolClient : IAsyncDisposable
{
    private readonly TcpClient tcpClient = new();
    private readonly string host;
    private readonly int port;
    private NetworkStream? stream;
    private Arc4Provider? clientCipher;
    private Arc4Provider? serverCipher;
    private uint sequence;

    public StsProtocolClient(string host, int port)
    {
        this.host = host;
        this.port = port;
    }

    public async Task ConnectAsync()
    {
        await tcpClient.ConnectAsync(host, port);
        stream = tcpClient.GetStream();
    }

    public async Task<string> LoginAndRequestGameToken(string user, string password)
    {
        StsResponse loginStart = await Send("/Auth/LoginStart",
            Request(("LoginName", user), ("NetAddress", "127.0.0.1")));

        Srp6Client srp = Srp6Client.FromLoginStart(user, password, loginStart.Xml.Element("KeyData")!.Value);
        StsResponse keyData = await Send("/Auth/KeyData",
            Request(("KeyData", srp.BuildClientKeyData())));

        srp.AcceptServerEvidence(keyData.Xml.Element("KeyData")?.Value);
        clientCipher = new Arc4Provider(srp.SessionKey);
        serverCipher = new Arc4Provider(srp.SessionKey);

        await Send("/Auth/LoginFinish",
            Request(("LongTermSession", ""), ("SecondaryAuthToken", ""), ("RegisterVerifiedIp", "false")));

        await Send("/GameAccount/ListMyAccounts",
            Request(("UserId", user), ("GameCode", "WildStar")));

        StsResponse token = await Send("/Auth/RequestGameToken",
            Request(("UserId", user), ("GameCode", "WildStar"), ("AccountAlias", user)));

        return token.Xml.Element("Token")!.Value;
    }

    private async Task<StsResponse> Send(string uri, string body)
    {
        if (stream == null)
            throw new InvalidOperationException("STS client is not connected.");

        byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
        string header = $"POST {uri} STS/1.0\r\nl:{bodyBytes.Length}\r\ns:{++sequence}\r\n\r\n";
        byte[] headerBytes = Encoding.UTF8.GetBytes(header);
        byte[] packet = new byte[headerBytes.Length + bodyBytes.Length];
        Buffer.BlockCopy(headerBytes, 0, packet, 0, headerBytes.Length);
        Buffer.BlockCopy(bodyBytes, 0, packet, headerBytes.Length, bodyBytes.Length);

        clientCipher?.Encrypt(packet);
        await stream.WriteAsync(packet);
        await stream.FlushAsync();

        return await ReadResponse();
    }

    private async Task<StsResponse> ReadResponse()
    {
        if (stream == null)
            throw new InvalidOperationException("STS client is not connected.");

        List<byte> header = new();
        while (header.Count < 4096)
        {
            header.Add(await ReadByte());
            if (header.Count >= 4 &&
                header[^4] == '\r' &&
                header[^3] == '\n' &&
                header[^2] == '\r' &&
                header[^1] == '\n')
                break;
        }

        string headerText = Encoding.UTF8.GetString(header.ToArray());
        Dictionary<string, string> headers = ParseStsHeaders(headerText);
        int length = int.Parse(headers["l"], CultureInfo.InvariantCulture);

        byte[] body = new byte[length];
        for (int i = 0; i < body.Length; i++)
            body[i] = await ReadByte();

        string bodyText = Encoding.UTF8.GetString(body).Trim();
        return new StsResponse(XElement.Parse(bodyText));
    }

    private async Task<byte> ReadByte()
    {
        byte[] buffer = new byte[1];
        int read = await stream!.ReadAsync(buffer);
        if (read != 1)
            throw new IOException("Unexpected end of STS stream.");

        serverCipher?.Decrypt(buffer);
        return buffer[0];
    }

    private static Dictionary<string, string> ParseStsHeaders(string headerText)
    {
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
        foreach (string line in headerText.Split(["\r\n"], StringSplitOptions.None).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            int separator = line.IndexOf(':');
            if (separator > 0)
                headers[line[..separator]] = line[(separator + 1)..];
        }

        return headers;
    }

    private static string Request(params (string Name, string Value)[] values)
    {
        var element = new XElement("Request",
            values.Select(v => new XElement(v.Name, v.Value)));
        return element.ToString(SaveOptions.DisableFormatting);
    }

    public ValueTask DisposeAsync()
    {
        stream?.Dispose();
        tcpClient.Dispose();
        return ValueTask.CompletedTask;
    }
}

internal sealed record StsResponse(XElement Xml);

internal sealed class GameProtocolClient : IAsyncDisposable
{
    private readonly TcpClient tcpClient = new();
    private readonly string host;
    private readonly int port;
    private PacketCrypt crypt;
    private NetworkStream? stream;

    public GameProtocolClient(string host, int port, ulong cryptKey)
    {
        this.host = host;
        this.port = port;
        crypt = new PacketCrypt(cryptKey);
    }

    public async Task ConnectAsync()
    {
        await tcpClient.ConnectAsync(host, port);
        stream = tcpClient.GetStream();
    }

    public void SetCrypt(ulong cryptKey)
    {
        crypt = new PacketCrypt(cryptKey);
    }

    public async Task SendEncrypted(GameMessageOpcode opcode, Action<GamePacketWriter> writeBody)
    {
        byte[] inner = PacketWriters.BuildInner(opcode, writeBody);
        byte[] encrypted = crypt.Encrypt(inner, inner.Length);
        byte[] outer = PacketWriters.BuildFrame(GameMessageOpcode.ClientEncrypted, writer =>
        {
            writer.Write((uint)encrypted.Length + 4u);
            writer.WriteBytes(encrypted);
        });

        await stream!.WriteAsync(outer);
        await stream.FlushAsync();
    }

    public async Task WaitForOuterOpcode(GameMessageOpcode opcode, TimeSpan timeout)
    {
        using CancellationTokenSource cts = new(timeout);
        while (!cts.IsCancellationRequested)
        {
            GameFrame frame = await ReadFrame(cts.Token);
            if (frame.Opcode == opcode)
                return;
        }

        throw new TimeoutException($"Timed out waiting for {opcode}.");
    }

    public async Task WaitForInnerOpcode(GameMessageOpcode opcode, TimeSpan timeout)
    {
        using CancellationTokenSource cts = new(timeout);
        while (!cts.IsCancellationRequested)
        {
            GameFrame frame = await ReadFrame(cts.Token);
            if (frame.Opcode == opcode)
                return;

            if (frame.Opcode is GameMessageOpcode.ServerAuthEncrypted or GameMessageOpcode.ServerRealmEncrypted)
            {
                GameFrame inner = DecryptInner(frame);
                if (inner.Opcode == opcode)
                    return;
            }
        }

        throw new TimeoutException($"Timed out waiting for {opcode}.");
    }

    public async Task<RealmTicket> WaitForRealmTicket(TimeSpan timeout)
    {
        using CancellationTokenSource cts = new(timeout);
        while (!cts.IsCancellationRequested)
        {
            GameFrame frame = await ReadFrame(cts.Token);
            if (frame.Opcode != GameMessageOpcode.ServerAuthEncrypted)
                continue;

            GameFrame inner = DecryptInner(frame);
            if (inner.Opcode == GameMessageOpcode.ServerAuthDenied)
            {
                using var deniedStream = new MemoryStream(inner.Body);
                using var deniedReader = new GamePacketReader(deniedStream);
                var result = deniedReader.ReadEnum<NpLoginResult>(32u);
                throw new InvalidOperationException($"Auth denied: {result}");
            }

            if (inner.Opcode != GameMessageOpcode.ServerRealmInfo)
                continue;

            using var stream = new MemoryStream(inner.Body);
            using var reader = new GamePacketReader(stream);
            uint address = reader.ReadUInt();
            ushort port = reader.ReadUShort();
            byte[] sessionKey = reader.ReadBytes(16u);
            uint accountId = reader.ReadUInt();
            return new RealmTicket(accountId, sessionKey, address, port);
        }

        throw new TimeoutException("Timed out waiting for ServerRealmInfo.");
    }

    private GameFrame DecryptInner(GameFrame frame)
    {
        using var stream = new MemoryStream(frame.Body);
        using var reader = new GamePacketReader(stream);
        uint length = reader.ReadUInt();
        byte[] encrypted = reader.ReadBytes(length - 4u);
        byte[] decrypted = crypt.Decrypt(encrypted, encrypted.Length);
        return PacketWriters.ParsePayload(decrypted);
    }

    private async Task<GameFrame> ReadFrame(CancellationToken cancellationToken)
    {
        byte[] sizeBytes = await ReadExact(4, cancellationToken);
        uint size = BinaryPrimitives.ReadUInt32LittleEndian(sizeBytes);
        byte[] payload = await ReadExact((int)size - 4, cancellationToken);
        return PacketWriters.ParsePayload(payload);
    }

    private async Task<byte[]> ReadExact(int length, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[length];
        int offset = 0;
        while (offset < length)
        {
            int read = await stream!.ReadAsync(buffer.AsMemory(offset, length - offset), cancellationToken);
            if (read == 0)
                throw new IOException("Unexpected end of game stream.");

            offset += read;
        }

        return buffer;
    }

    public ValueTask DisposeAsync()
    {
        stream?.Dispose();
        tcpClient.Dispose();
        return ValueTask.CompletedTask;
    }
}

internal sealed record GameFrame(GameMessageOpcode Opcode, byte[] Body);

internal static class PacketWriters
{
    public static byte[] BuildInner(GameMessageOpcode opcode, Action<GamePacketWriter> writeBody)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        writer.Write(opcode, 16u);
        writeBody(writer);
        writer.FlushBits();
        return stream.ToArray();
    }

    public static byte[] BuildFrame(GameMessageOpcode opcode, Action<GamePacketWriter> writeBody)
    {
        using var bodyStream = new MemoryStream();
        using (var bodyWriter = new GamePacketWriter(bodyStream))
        {
            writeBody(bodyWriter);
            bodyWriter.FlushBits();
        }

        byte[] body = bodyStream.ToArray();
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        writer.Write((uint)(6 + body.Length));
        writer.Write(opcode, 16u);
        writer.WriteBytes(body);
        writer.FlushBits();
        return stream.ToArray();
    }

    public static GameFrame ParsePayload(byte[] payload)
    {
        using var stream = new MemoryStream(payload);
        using var reader = new GamePacketReader(stream);
        GameMessageOpcode opcode = reader.ReadEnum<GameMessageOpcode>(16u);
        byte[] body = reader.ReadBytes(reader.BytesRemaining);
        return new GameFrame(opcode, body);
    }

    public static void WriteClientHelloAuth(GamePacketWriter writer, string account, string token)
    {
        writer.Write(16042u);
        writer.Write(0x1588ul);
        writer.WriteStringFixed(account);
        WriteGuid(writer, Guid.Empty);
        WriteGuid(writer, Guid.Parse(token));
        writer.Write(0u);
        writer.Write(Language.English, 32u);
        writer.Write(0u);
        writer.Write(0u);
        WriteHardware(writer);
        writer.Write(9u);
    }

    public static void WriteClientHelloRealm(GamePacketWriter writer, uint accountId, byte[] sessionKey, string account)
    {
        writer.Write(accountId);
        writer.WriteBytes(sessionKey, 16u);
        writer.Write(0ul);
        writer.WriteStringWide(account);
        writer.Write(3u);
    }

    private static void WriteHardware(GamePacketWriter writer)
    {
        writer.WriteStringWide("LoadTest");
        writer.WriteStringWide("Virtual CPU");
        writer.WriteStringWide("NexusForever.LoadTest");
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(3200u);
        writer.Write(4u);
        writer.Write(8192u);
        writer.WriteStringWide("Virtual GPU");
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(1024u);
        writer.Write(0x10009u);
        writer.Write(0x000A0000u);
        writer.Write(0u);
        writer.Write(1u);
    }

    private static void WriteGuid(GamePacketWriter writer, Guid guid)
    {
        byte[] bytes = guid.ToByteArray();
        writer.Write(BitConverter.ToInt32(bytes, 0));
        writer.Write(BitConverter.ToUInt16(bytes, 4));
        writer.Write(BitConverter.ToUInt16(bytes, 6));
        writer.WriteBytes(bytes.Skip(8).ToArray(), 8u);
    }
}

internal sealed class Srp6Client
{
    private static readonly BigInteger g = 2;
    private static readonly BigInteger N = new(new byte[] {
        0xE3, 0x06, 0xEB, 0xC0, 0x2F, 0x1D, 0xC6, 0x9F, 0x5B, 0x43, 0x76, 0x83, 0xFE, 0x38, 0x51, 0xFD,
        0x9A, 0xAA, 0x6E, 0x97, 0xF4, 0xCB, 0xD4, 0x2F, 0xC0, 0x6C, 0x72, 0x05, 0x3C, 0xBC, 0xED, 0x68,
        0xEC, 0x57, 0x0E, 0x66, 0x66, 0xF5, 0x29, 0xC5, 0x85, 0x18, 0xCF, 0x7B, 0x29, 0x9B, 0x55, 0x82,
        0x49, 0x5D, 0xB1, 0x69, 0xAD, 0xF4, 0x8E, 0xCE, 0xB6, 0xD6, 0x54, 0x61, 0xB4, 0xD7, 0xC7, 0x5D,
        0xD1, 0xDA, 0x89, 0x60, 0x1D, 0x5C, 0x49, 0x8E, 0xE4, 0x8B, 0xB9, 0x50, 0xE2, 0xD8, 0xD5, 0xE0,
        0xE0, 0xC6, 0x92, 0xD6, 0x13, 0x48, 0x3B, 0x38, 0xD3, 0x81, 0xEA, 0x96, 0x74, 0xDF, 0x74, 0xD6,
        0x76, 0x65, 0x25, 0x9C, 0x4C, 0x31, 0xA2, 0x9E, 0x0B, 0x3C, 0xFF, 0x75, 0x87, 0x61, 0x72, 0x60,
        0xE8, 0xC5, 0x8F, 0xFA, 0x0A, 0xF8, 0x33, 0x9C, 0xD6, 0x8D, 0xB3, 0xAD, 0xB9, 0x0A, 0xAF, 0xEE }, true);

    public byte[] SessionKey { get; }

    private readonly BigInteger A;
    private readonly BigInteger M1;

    private Srp6Client(BigInteger A, BigInteger M1, byte[] sessionKey)
    {
        this.A = A;
        this.M1 = M1;
        SessionKey = sessionKey;
    }

    public static Srp6Client FromLoginStart(string user, string password, string keyData)
    {
        using var stream = new MemoryStream(Convert.FromBase64String(keyData));
        using var reader = new BinaryReader(stream);
        byte[] saltBytes = reader.ReadBytes(reader.ReadInt32());
        byte[] serverBBytes = reader.ReadBytes(reader.ReadInt32());

        BigInteger s = new(saltBytes, true);
        BigInteger B = new(serverBBytes, true);
        BigInteger a = RandomUnsignedBigInteger(128);
        BigInteger A = BigInteger.ModPow(g, a, N);

        using SHA256 sha256 = SHA256.Create();
        BigInteger passwordHash = new(sha256.ComputeHash(Encoding.UTF8.GetBytes($"{user}:{password}")), true);
        BigInteger x = Hash(true, s, passwordHash);
        BigInteger u = Hash(true, A, B);
        BigInteger k = Hash(true, N, g);
        BigInteger gx = BigInteger.ModPow(g, x, N);
        BigInteger baseValue = (B - (k * gx)) % N;
        if (baseValue.Sign < 0)
            baseValue += N;

        BigInteger S = BigInteger.ModPow(baseValue, a + (u * x), N);
        BigInteger K = ShaInterleave(S);
        byte[] sessionKey = K.ToByteArray(true);

        byte[] userHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(user));
        BigInteger M1 = Hash(false, Hash(false, N) ^ Hash(false, g), new BigInteger(userHash, true), s, A, B, K);
        return new Srp6Client(A, M1, sessionKey);
    }

    public string BuildClientKeyData()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        byte[] aBytes = A.ToByteArray(true);
        byte[] m1Bytes = M1.ToByteArray(true);
        writer.Write(aBytes.Length);
        writer.Write(aBytes);
        writer.Write(m1Bytes.Length);
        writer.Write(m1Bytes);
        return Convert.ToBase64String(stream.ToArray());
    }

    public void AcceptServerEvidence(string? _)
    {
        // Server evidence is not needed to generate the transport key. The server
        // already disconnects or rejects the next encrypted message if M1 is wrong.
    }

    private static BigInteger RandomUnsignedBigInteger(int bytes)
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(bytes);
        return new BigInteger(buffer, true);
    }

    private static BigInteger Hash(bool reverse, params BigInteger[] integers)
    {
        using SHA256 sha256 = SHA256.Create();
        sha256.Initialize();

        for (int i = 0; i < integers.Length; i++)
        {
            byte[] buffer = integers[i].ToByteArray(true);
            int padding = buffer.Length % 4;
            if (padding != 0)
                Array.Resize(ref buffer, buffer.Length + (4 - padding));

            if (i == integers.Length - 1)
                sha256.TransformFinalBlock(buffer, 0, buffer.Length);
            else
                sha256.TransformBlock(buffer, 0, buffer.Length, null, 0);
        }

        byte[] hash = sha256.Hash!;
        if (reverse)
            ReverseBytesAsUInt32(hash);
        return new BigInteger(hash, true);
    }

    private static BigInteger ShaInterleave(BigInteger key)
    {
        byte[] keyBytes = key.ToByteArray(true);
        byte[] T = keyBytes.Reverse().ToArray();
        int first0 = Array.IndexOf<byte>(keyBytes, 0);
        int length = 4;

        if (first0 >= 0 && first0 < T.Length - 4)
            length = T.Length - first0;

        byte[] E = new byte[length / 2];
        for (uint i = 0u; i < E.Length; i++)
            E[i] = T[i * 2];

        byte[] F = new byte[length / 2];
        for (uint i = 0u; i < F.Length; i++)
            F[i] = T[i * 2 + 1];

        using SHA256 sha256 = SHA256.Create();
        byte[] G = sha256.ComputeHash(E);
        byte[] H = sha256.ComputeHash(F);

        byte[] K = new byte[G.Length + H.Length];
        for (uint i = 0u; i < K.Length; i++)
            K[i] = i % 2 == 0 ? G[i / 2] : H[i / 2];

        return new BigInteger(K, true);
    }

    private static void ReverseBytesAsUInt32(byte[] array)
    {
        int j = array.Length - 4;
        for (int i = 0; i < array.Length / 2; i += 4, j -= 4)
        {
            (array[i + 0], array[j + 0]) = (array[j + 0], array[i + 0]);
            (array[i + 1], array[j + 1]) = (array[j + 1], array[i + 1]);
            (array[i + 2], array[j + 2]) = (array[j + 2], array[i + 2]);
            (array[i + 3], array[j + 3]) = (array[j + 3], array[i + 3]);
        }
    }
}

internal static class ReportCommand
{
    public static async Task<int> Run(CliArgs args)
    {
        string input = args.GetString("input", Path.Combine("artifacts", "load-tests"));
        List<LoginWorldSample> samples = new();

        IEnumerable<string> files = File.Exists(input)
            ? [input]
            : Directory.EnumerateFiles(input, "*.json", SearchOption.AllDirectories);

        foreach (string file in files)
        {
            string json = await File.ReadAllTextAsync(file);
            LoadTestRunReport? report = JsonSerializer.Deserialize<LoadTestRunReport>(json);
            if (report?.Samples != null)
                samples.AddRange(report.Samples);
        }

        Console.WriteLine(Summarise(samples).ToMarkdown());
        return samples.Any(s => !s.Success) ? 2 : 0;
    }

    public static LoadTestSummary Summarise(IReadOnlyCollection<LoginWorldSample> samples)
    {
        var summary = new LoadTestSummary
        {
            Samples = samples.Count,
            Successes = samples.Count(s => s.Success),
            Failures = samples.Count(s => !s.Success),
            Total = Percentiles(samples.Select(s => s.TotalMs))
        };

        foreach (IGrouping<string, LoadTestStep> group in samples.SelectMany(s => s.Steps).GroupBy(s => s.Name))
            summary.Steps[group.Key] = Percentiles(group.Select(s => s.DurationMs));

        summary.Bottlenecks = summary.Steps
            .OrderByDescending(s => s.Value.P95)
            .Take(5)
            .Select(s => $"{s.Key}: p95 {s.Value.P95:F1} ms")
            .ToList();

        return summary;
    }

    private static PercentileSummary Percentiles(IEnumerable<double> values)
    {
        double[] sorted = values.OrderBy(v => v).ToArray();
        if (sorted.Length == 0)
            return new PercentileSummary();

        return new PercentileSummary
        {
            P50 = Pick(sorted, 0.50),
            P95 = Pick(sorted, 0.95),
            P99 = Pick(sorted, 0.99),
            Max = sorted[^1]
        };
    }

    private static double Pick(double[] sorted, double percentile)
    {
        int index = Math.Clamp((int)Math.Ceiling(sorted.Length * percentile) - 1, 0, sorted.Length - 1);
        return sorted[index];
    }
}

internal sealed class LoadTestRunReport
{
    public string Scenario { get; set; } = "";
    public DateTime StartedUtc { get; set; }
    public DateTime CompletedUtc { get; set; }
    public Dictionary<string, string> Options { get; set; } = new();
    public List<LoginWorldSample> Samples { get; set; } = new();
    public LoadTestSummary Summary { get; set; } = new();
}

internal sealed class LoginWorldSample
{
    public string User { get; set; } = "";
    public string Label { get; set; } = "";
    public DateTime StartedUtc { get; set; }
    public DateTime CompletedUtc { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public double TotalMs { get; set; }
    public List<LoadTestStep> Steps { get; set; } = new();
}

internal sealed class LoadTestStep
{
    public string Name { get; set; } = "";
    public double DurationMs { get; set; }
}

internal sealed class LoadTestSummary
{
    public int Samples { get; set; }
    public int Successes { get; set; }
    public int Failures { get; set; }
    public PercentileSummary Total { get; set; } = new();
    public Dictionary<string, PercentileSummary> Steps { get; set; } = new();
    public List<string> Bottlenecks { get; set; } = new();

    public string ToMarkdown()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"samples={Samples} successes={Successes} failures={Failures}");
        builder.AppendLine($"total p50={Total.P50:F1}ms p95={Total.P95:F1}ms p99={Total.P99:F1}ms max={Total.Max:F1}ms");
        foreach ((string step, PercentileSummary value) in Steps.OrderBy(s => s.Key))
            builder.AppendLine($"{step} p50={value.P50:F1}ms p95={value.P95:F1}ms p99={value.P99:F1}ms max={value.Max:F1}ms");

        if (Bottlenecks.Count != 0)
        {
            builder.AppendLine("top bottlenecks:");
            foreach (string bottleneck in Bottlenecks)
                builder.AppendLine($"- {bottleneck}");
        }

        return builder.ToString();
    }
}

internal sealed class PercentileSummary
{
    public double P50 { get; set; }
    public double P95 { get; set; }
    public double P99 { get; set; }
    public double Max { get; set; }
}
