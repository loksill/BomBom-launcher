using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace SS14.Launcher.Models.Helix;

public sealed class HelixDiscordRichPresence
{
    public const string DiscordUrl = ConfigConstants.BomBomDiscordUrl;

    private const string ClientId = "1502326436168597815";
    private const string LargeImageKey = "helix";
    private const int ProtocolVersion = 1;
    private const int MaxIpcPipe = 10;
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);

    public static HelixDiscordRichPresence Instance { get; } = new();

    private readonly SemaphoreSlim _rpcLock = new(1, 1);
    private readonly long _startedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    private Stream? _stream;
    private Socket? _socket;
    private DateTime _lastConnectAttempt = DateTime.MinValue;
    private HelixDiscordActivity? _activity;

    private HelixDiscordRichPresence()
    {
    }

    public void SetActivity(string state)
    {
        SetActivity(new HelixDiscordActivity(State: state));
    }

    public void SetActivity(HelixDiscordActivity activity)
    {
        activity = activity.Normalize();

        if (_activity == activity && _stream is { CanWrite: true })
            return;

        _activity = activity;
        _ = SetActivityAsync();
    }

    public void Stop()
    {
        _ = StopAsync();
    }

    private async Task SetActivityAsync()
    {
        await _rpcLock.WaitAsync();

        try
        {
            using var cancel = new CancellationTokenSource(RequestTimeout);

            if (!await EnsureConnected())
                return;

            var current = _activity ?? new HelixDiscordActivity(State: "In launcher");
            var activity = new JsonObject
            {
                ["state"] = current.State,
                ["timestamps"] = new JsonObject
                {
                    ["start"] = _startedAtUnix
                },
                ["assets"] = new JsonObject
                {
                    ["large_image"] = LargeImageKey,
                    ["large_text"] = "BomBom Launcher"
                },
                ["buttons"] = new JsonArray(
                    new JsonObject
                    {
                        ["label"] = "BomBom Discord",
                        ["url"] = DiscordUrl
                    })
            };

            if (!string.IsNullOrWhiteSpace(current.Details))
                activity["details"] = current.Details;

            if (current.PlayerCount is { } playerCount && current.MaxPlayers is { } maxPlayers)
            {
                activity["party"] = new JsonObject
                {
                    ["size"] = new JsonArray(JsonValue.Create(playerCount), JsonValue.Create(maxPlayers))
                };
            }

            var payload = new JsonObject
            {
                ["cmd"] = "SET_ACTIVITY",
                ["args"] = new JsonObject
                {
                    ["pid"] = Environment.ProcessId,
                    ["activity"] = activity
                },
                ["nonce"] = Guid.NewGuid().ToString("N")
            };

            await SendPayload(DiscordIpcOp.Frame, payload, cancel.Token);
            await ReadFrame(cancel.Token);
        }
        catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            Log.Debug(e, "BomBom Discord RPC update failed");
            CloseConnection();
        }
        finally
        {
            _rpcLock.Release();
        }
    }

    private async Task StopAsync()
    {
        await _rpcLock.WaitAsync();

        try
        {
            using var cancel = new CancellationTokenSource(RequestTimeout);

            if (_stream is { CanWrite: true })
            {
                var payload = new JsonObject
                {
                    ["cmd"] = "SET_ACTIVITY",
                    ["args"] = new JsonObject
                    {
                        ["pid"] = Environment.ProcessId,
                        ["activity"] = null
                    },
                    ["nonce"] = Guid.NewGuid().ToString("N")
                };

                await SendPayload(DiscordIpcOp.Frame, payload, cancel.Token);
            }
        }
        catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException)
        {
            Log.Debug(e, "BomBom Discord RPC clear failed");
        }
        finally
        {
            CloseConnection();
            _rpcLock.Release();
        }
    }

    private async Task<bool> EnsureConnected()
    {
        if (_stream is { CanWrite: true })
            return true;

        if (DateTime.UtcNow - _lastConnectAttempt < ReconnectDelay)
            return false;

        _lastConnectAttempt = DateTime.UtcNow;
        CloseConnection();

        foreach (var candidate in GetIpcCandidates())
        {
            try
            {
                _stream = await Connect(candidate);
                using var cancel = new CancellationTokenSource(RequestTimeout);

                var handshake = new JsonObject
                {
                    ["v"] = ProtocolVersion,
                    ["client_id"] = ClientId
                };

                await SendPayload(DiscordIpcOp.Handshake, handshake, cancel.Token);
                await ReadFrame(cancel.Token);

                Log.Debug("BomBom Discord RPC connected via {Candidate}", candidate.DisplayName);
                return true;
            }
            catch (Exception e) when (e is IOException or SocketException or TimeoutException or OperationCanceledException)
            {
                Log.Verbose(e, "BomBom Discord RPC failed to connect via {Candidate}", candidate.DisplayName);
                CloseConnection();
            }
        }

        return false;
    }

    private async Task<Stream> Connect(IpcCandidate candidate)
    {
        using var cancel = new CancellationTokenSource(ConnectTimeout);

        if (candidate.PipeName != null)
        {
            var pipe = new NamedPipeClientStream(".", candidate.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                await pipe.ConnectAsync(cancel.Token);
                return pipe;
            }
            catch
            {
                await pipe.DisposeAsync();
                throw;
            }
        }

        Debug.Assert(candidate.SocketPath != null);

        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(candidate.SocketPath), cancel.Token);
            _socket = socket;
            return new NetworkStream(socket, ownsSocket: false);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private async Task SendPayload(DiscordIpcOp op, JsonObject payload, CancellationToken cancel = default)
    {
        if (_stream == null)
            throw new IOException("Discord RPC stream is not connected");

        var json = payload.ToJsonString();
        var body = Encoding.UTF8.GetBytes(json);
        var header = new byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0, 4), (int)op);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4, 4), body.Length);

        await _stream.WriteAsync(header, cancel);
        await _stream.WriteAsync(body, cancel);
        await _stream.FlushAsync(cancel);
    }

    private async Task<string> ReadFrame(CancellationToken cancel = default)
    {
        if (_stream == null)
            throw new IOException("Discord RPC stream is not connected");

        while (true)
        {
            var header = new byte[8];
            await _stream.ReadExactlyAsync(header, cancel);

            var op = (DiscordIpcOp)BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(0, 4));
            var length = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4, 4));
            if (length < 0)
                throw new IOException("Discord RPC returned an invalid payload length");

            var body = new byte[length];
            await _stream.ReadExactlyAsync(body, cancel);

            var payload = Encoding.UTF8.GetString(body);

            switch (op)
            {
                case DiscordIpcOp.Ping:
                    await SendPayload(DiscordIpcOp.Pong, JsonNode.Parse(payload) as JsonObject ?? new JsonObject(), cancel);
                    continue;

                case DiscordIpcOp.Close:
                    throw new IOException($"Discord RPC closed the connection: {payload}");

                default:
                    return payload;
            }
        }
    }

    private void CloseConnection()
    {
        _stream?.Dispose();
        _stream = null;

        _socket?.Dispose();
        _socket = null;
    }

    private static string NormalizeState(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return "In launcher";

        state = state.Trim();
        return state.Length <= 128 ? state : state[..128];
    }

    private static IEnumerable<IpcCandidate> GetIpcCandidates()
    {
        if (OperatingSystem.IsWindows())
        {
            for (var i = 0; i < MaxIpcPipe; i++)
                yield return IpcCandidate.Pipe($"discord-ipc-{i}");

            yield break;
        }

        var baseDirs = new[]
        {
            Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR"),
            Environment.GetEnvironmentVariable("TMPDIR"),
            Environment.GetEnvironmentVariable("TMP"),
            Environment.GetEnvironmentVariable("TEMP"),
            "/tmp"
        };

        foreach (var baseDir in baseDirs.Where(d => !string.IsNullOrWhiteSpace(d)).Distinct())
        {
            for (var i = 0; i < MaxIpcPipe; i++)
                yield return IpcCandidate.Socket(Path.Combine(baseDir!, $"discord-ipc-{i}"));
        }
    }

    private readonly record struct IpcCandidate(string? PipeName, string? SocketPath)
    {
        public string DisplayName => PipeName ?? SocketPath ?? "<unknown>";

        public static IpcCandidate Pipe(string pipeName)
        {
            return new IpcCandidate(pipeName, null);
        }

        public static IpcCandidate Socket(string socketPath)
        {
            return new IpcCandidate(null, socketPath);
        }
    }

    private enum DiscordIpcOp
    {
        Handshake = 0,
        Frame = 1,
        Close = 2,
        Ping = 3,
        Pong = 4
    }
}

public sealed record HelixDiscordActivity(
    string? Details = null,
    string State = "In launcher",
    int? PlayerCount = null,
    int? MaxPlayers = null)
{
    public HelixDiscordActivity Normalize()
    {
        var state = string.IsNullOrWhiteSpace(State) ? "In launcher" : State;
        var playerCount = PlayerCount;
        var maxPlayers = MaxPlayers;

        if (playerCount is < 0 || maxPlayers is <= 0 || playerCount > maxPlayers)
        {
            playerCount = null;
            maxPlayers = null;
        }

        return this with
        {
            Details = NormalizeOptional(Details),
            State = NormalizeState(state),
            PlayerCount = playerCount,
            MaxPlayers = maxPlayers
        };
    }

    private static string NormalizeState(string state)
    {
        state = state.Trim();
        return state.Length <= 128 ? state : state[..128];
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : NormalizeState(value);
    }
}
