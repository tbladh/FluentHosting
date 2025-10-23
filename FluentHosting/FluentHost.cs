using FluentHosting.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FluentHosting
{
    public class FluentHost : IAsyncDisposable, IDisposable
    {
        private readonly HttpListener _listener;
        private readonly ReaderWriterLockSlim _handlersLock = new ReaderWriterLockSlim();
        private readonly List<IRouteHandler> _handlers = new List<IRouteHandler>();
        private readonly SemaphoreSlim _lifecycleLock = new SemaphoreSlim(1, 1);

        private CancellationTokenSource _cts;
        private Task _listenLoop;
        private bool _prefixRegistered;
        private HostState _state = HostState.Stopped;
        private bool _disposed;

        public FluentHost(string name, int port)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Port = port;
            _listener = new HttpListener();
        }

        public string Name { get; }

        public int Port { get; }

        public IReadOnlyCollection<IRouteHandler> Handlers
        {
            get
            {
                _handlersLock.EnterReadLock();
                try
                {
                    return _handlers.ToArray();
                }
                finally
                {
                    _handlersLock.ExitReadLock();
                }
            }
        }

        public FluentHost Start()
        {
            StartAsync().GetAwaiter().GetResult();
            return this;
        }

        public async Task<FluentHost> StartAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_state is HostState.Running or HostState.Starting)
                {
                    throw new InvalidOperationException("Host is already running.");
                }

                if (_state == HostState.Stopping)
                {
                    throw new InvalidOperationException("Host is currently stopping.");
                }

                _state = HostState.Starting;

                if (!_prefixRegistered)
                {
                    _listener.Prefixes.Add($"{Name}:{Port}/");
                    _prefixRegistered = true;
                }

                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _listener.Start();
                _listenLoop = Task.Run(() => ListenAsync(_cts.Token), CancellationToken.None);
                _state = HostState.Running;
            }
            finally
            {
                _lifecycleLock.Release();
            }

            return this;
        }

        public void Stop()
        {
            StopAsync().GetAwaiter().GetResult();
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_state is HostState.Stopped or HostState.Disposed)
                {
                    return;
                }

                _state = HostState.Stopping;

                try
                {
                    _cts?.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }

                if (_listener.IsListening)
                {
                    try
                    {
                        _listener.Stop();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                    catch (HttpListenerException)
                    {
                        // Listener already closed.
                    }
                }
            }
            finally
            {
                _lifecycleLock.Release();
            }

            if (_listenLoop != null)
            {
                try
                {
                    await _listenLoop.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                }
                catch (HttpListenerException)
                {
                }
            }

            await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _cts?.Dispose();
                _cts = null;
                _listenLoop = null;
                if (_state != HostState.Disposed)
                {
                    _state = HostState.Stopped;
                }
            }
            finally
            {
                _lifecycleLock.Release();
            }
        }

        public void Dispose()
        {
            DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            await StopAsync().ConfigureAwait(false);

            _lifecycleLock.Dispose();
            _handlersLock.Dispose();
            _listener.Close();
            _disposed = true;
            _state = HostState.Disposed;
        }

        internal void RegisterHandler(IRouteHandler handler, bool isFallback)
        {
            if (handler is null) throw new ArgumentNullException(nameof(handler));

            _handlersLock.EnterWriteLock();
            try
            {
                if (isFallback)
                {
                    _handlers.Add(handler);
                }
                else
                {
                    _handlers.Insert(0, handler);
                }
            }
            finally
            {
                _handlersLock.ExitWriteLock();
            }
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context = null;

                try
                {
                    context = await _listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (HttpListenerException ex) when (IsListenerShutdown(ex))
                {
                    break;
                }
                catch (InvalidOperationException)
                {
                    if (!_listener.IsListening)
                    {
                        break;
                    }

                    continue;
                }
                catch when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch
                {
                    continue;
                }

                if (context != null)
                {
                    _ = Task.Run(() => ProcessRequestAsync(context), CancellationToken.None);
                }
            }
        }

        private static bool IsListenerShutdown(HttpListenerException exception)
        {
            // 995 = ERROR_OPERATION_ABORTED, 500 = ERROR_NO_SESSION when closing
            return exception.ErrorCode is 995 or 500;
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            try
            {
                var route = context.Request.Url?.LocalPath ?? "/";
                var verb = context.Request.HttpMethod.ToVerb();

                var handler = FindHandler(route, verb);

                if (handler is null)
                {
                    context.Response.StatusCode = 404;
                    context.Response.ContentLength64 = 0;
                    context.Response.Close();
                    return;
                }

                await WriteResponseAsync(handler, context).ConfigureAwait(false);
                context.Response.Close();
            }
            catch (Exception)
            {
                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentLength64 = 0;
                    context.Response.Close();
                }
                catch
                {
                    // ignored
                }
            }
        }

        private IRouteHandler FindHandler(string route, Verb verb)
        {
            _handlersLock.EnterReadLock();
            try
            {
                foreach (var handler in _handlers)
                {
                    if (IsRouteMatch(handler.Route, route) && (handler.Verb & verb) != 0)
                    {
                        return handler;
                    }
                }

                return null;
            }
            finally
            {
                _handlersLock.ExitReadLock();
            }
        }

        private async Task WriteResponseAsync(IRouteHandler handler, HttpListenerContext context)
        {
            var headers = context.Request.Headers;
            var requestOrigin = headers["Origin"];
            if (requestOrigin != null && handler.CorsConfig != null)
            {
                var corsHeaders = handler.CorsConfig.ToHeaders(requestOrigin);
                foreach (var corsHeader in corsHeaders)
                {
                    context.Response.Headers.Add(corsHeader);
                }
            }

            var hostResponse = handler.Handler.Invoke(context);
            context.Response.ContentEncoding = hostResponse.Encoding;
            context.Response.ContentType = hostResponse.ContentType;
            context.Response.ContentLength64 = hostResponse.ContentLength;
            context.Response.StatusCode = hostResponse.Code;

            try
            {
                await hostResponse.Stream.CopyToAsync(context.Response.OutputStream).ConfigureAwait(false);
            }
            finally
            {
                hostResponse.Stream.Dispose();
            }
        }

        private static bool IsRouteMatch(string routePattern, string requestPath)
        {
            if (string.Equals(routePattern, requestPath, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (routePattern.EndsWith("*", StringComparison.InvariantCulture))
            {
                var basePath = routePattern.Substring(0, routePattern.Length - 1);
                return requestPath.StartsWith(basePath, StringComparison.InvariantCultureIgnoreCase);
            }

            return false;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FluentHost));
            }
        }

        private enum HostState
        {
            Stopped,
            Starting,
            Running,
            Stopping,
            Disposed
        }
    }
}
