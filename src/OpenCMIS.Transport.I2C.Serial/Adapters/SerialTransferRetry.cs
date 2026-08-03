using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Transport.I2C.Serial.Adapters
{
    internal sealed class SerialTransferRetry(I2cRetryOptions options,
                                              TimeProvider    timeProvider)
    {
        public async ValueTask<T> ExecuteAsync<T>(Func<CancellationToken, ValueTask<T>> operation,
                                                  CancellationToken                     cancellationToken)
        {
            for (var attempt = 1; attempt <= options.MaxAttempts; attempt++)
                try
                {
                    return await operation(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception)
                        when (IsTransient(exception) && attempt < options.MaxAttempts)
                {
                    await Task.Delay(
                                       options.Delay,
                                       timeProvider,
                                       cancellationToken)
                              .ConfigureAwait(false);
                }
                catch (Exception exception) when (IsTransient(exception))
                {
                    throw new CmisException(
                            CmisErrorCode.I2cTransferFailed,
                            exception);
                }

            throw new InvalidOperationException("The retry loop completed unexpectedly.");
        }

        private static bool IsTransient(Exception exception)
        {
            return exception is IOException or TimeoutException;
        }
    }
}
