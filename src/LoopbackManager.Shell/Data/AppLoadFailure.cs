using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;

namespace LoopbackManager.Shell;

/// <summary>Actionable categories for an app-list load failure.</summary>
public enum AppLoadFailureKind
{
    None,
    FirewallServiceUnavailable,
    AccessDenied,
    UnsupportedSystem,
    MissingSystemComponent,
    InvalidSystemConfiguration,
    ResourceExhausted,
    Unknown,
}

/// <summary>A classified load failure plus the original diagnostic detail users can include in a report.</summary>
/// <param name="Kind">The category used to select recovery guidance.</param>
/// <param name="Details">The exception type, message, and native error code when available.</param>
public readonly record struct AppLoadFailure(AppLoadFailureKind Kind, string Details)
{
    internal static AppLoadFailure From(Exception? error)
    {
        if (error is null)
        {
            return new(AppLoadFailureKind.None, string.Empty);
        }

        var (kind, diagnostic) = Classify(error);
        return new(kind, FormatDetails(diagnostic));
    }

    private static (AppLoadFailureKind Kind, Exception Diagnostic) Classify(Exception error)
    {
        foreach (var candidate in Enumerate(error))
        {
            var kind = ClassifySingle(candidate);
            if (kind != AppLoadFailureKind.Unknown)
            {
                return (kind, candidate);
            }
        }

        return (AppLoadFailureKind.Unknown, error);
    }

    private static AppLoadFailureKind ClassifySingle(Exception error)
    {
        if (error is Win32Exception win32)
        {
            return ClassifyNativeError(win32.NativeErrorCode);
        }

        if (error is ExternalException external && TryGetWin32Code(external.ErrorCode, out var nativeError))
        {
            return ClassifyNativeError(nativeError);
        }

        return error switch
        {
            UnauthorizedAccessException or SecurityException => AppLoadFailureKind.AccessDenied,
            DllNotFoundException or EntryPointNotFoundException or BadImageFormatException
                => AppLoadFailureKind.MissingSystemComponent,
            PlatformNotSupportedException => AppLoadFailureKind.UnsupportedSystem,
            InvalidDataException => AppLoadFailureKind.InvalidSystemConfiguration,
            OutOfMemoryException => AppLoadFailureKind.ResourceExhausted,
            _ => AppLoadFailureKind.Unknown,
        };
    }

    private static AppLoadFailureKind ClassifyNativeError(int error) => error switch
    {
        5 => AppLoadFailureKind.AccessDenied,
        8 or 14 => AppLoadFailureKind.ResourceExhausted,
        13 or 1336 or 1337 or 1338 => AppLoadFailureKind.InvalidSystemConfiguration,
        50 or 87 or 120 => AppLoadFailureKind.UnsupportedSystem,
        126 or 127 or 193 => AppLoadFailureKind.MissingSystemComponent,
        1058 or 1060 or 1062 or 1068 or 1075 or 1722 or 1753
            => AppLoadFailureKind.FirewallServiceUnavailable,
        _ => AppLoadFailureKind.Unknown,
    };

    private static IEnumerable<Exception> Enumerate(Exception error)
    {
        yield return error;

        if (error is AggregateException aggregate)
        {
            foreach (var inner in aggregate.Flatten().InnerExceptions)
            {
                foreach (var candidate in Enumerate(inner))
                {
                    yield return candidate;
                }
            }
        }
        else if (error.InnerException is { } inner)
        {
            foreach (var candidate in Enumerate(inner))
            {
                yield return candidate;
            }
        }
    }

    private static string FormatDetails(Exception error)
    {
        var message = string.IsNullOrWhiteSpace(error.Message) ? error.GetType().Name : error.Message.Trim();
        if (error is Win32Exception win32 && !message.Contains("0x", StringComparison.OrdinalIgnoreCase))
        {
            message = $"{message} (0x{unchecked((uint)win32.NativeErrorCode):X8})";
        }
        else if (error is ExternalException external && !message.Contains("0x", StringComparison.OrdinalIgnoreCase))
        {
            message = $"{message} (HRESULT 0x{unchecked((uint)external.ErrorCode):X8})";
        }

        return $"{error.GetType().Name}: {message}";
    }

    private static bool TryGetWin32Code(int hresult, out int error)
    {
        var value = unchecked((uint)hresult);
        if ((value & 0xFFFF0000u) == 0x80070000u)
        {
            error = (int)(value & 0xFFFFu);
            return true;
        }

        error = 0;
        return false;
    }
}
