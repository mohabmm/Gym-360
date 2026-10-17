'use client';

import { useEffect, useRef, useCallback, useState } from 'react';
import { HubConnectionBuilder, HubConnection, HubConnectionState, LogLevel } from '@microsoft/signalr';

interface UseSignalROptions {
  hubUrl: string;
  accessToken?: string | null;
  autoReconnect?: boolean;
  onConnected?: () => void;
  onDisconnected?: () => void;
  onReconnecting?: () => void;
}

interface UseSignalRResult {
  connection: HubConnection | null;
  connectionState: HubConnectionState;
  on: (event: string, callback: (...args: unknown[]) => void) => void;
  off: (event: string, callback: (...args: unknown[]) => void) => void;
  invoke: (method: string, ...args: unknown[]) => Promise<void>;
}

const RECONNECT_DELAYS_MS = [0, 2000, 5000, 10000, 30000];

function buildConnection(hubUrl: string, accessToken: string, autoReconnect: boolean): HubConnection {
  const builder = new HubConnectionBuilder()
    .withUrl(hubUrl, {
      accessTokenFactory: () => accessToken,
    })
    .configureLogging(LogLevel.Warning);

  if (autoReconnect) {
    builder.withAutomaticReconnect(RECONNECT_DELAYS_MS);
  }
  return builder.build();
}

type LifecycleCallbacks = Pick<UseSignalROptions, 'onConnected' | 'onDisconnected' | 'onReconnecting'>;

/** Mirrors hub lifecycle events into React state and the caller's callbacks. */
function wireLifecycle(
  connection: HubConnection,
  setConnectionState: (state: HubConnectionState) => void,
  { onConnected, onDisconnected, onReconnecting }: LifecycleCallbacks,
) {
  connection.onclose(() => {
    setConnectionState(HubConnectionState.Disconnected);
    onDisconnected?.();
  });

  connection.onreconnecting(() => {
    setConnectionState(HubConnectionState.Reconnecting);
    onReconnecting?.();
  });

  connection.onreconnected(() => {
    setConnectionState(HubConnectionState.Connected);
    onConnected?.();
  });
}

export function useSignalR(options: UseSignalROptions): UseSignalRResult {
  const { hubUrl, accessToken, autoReconnect = true, onConnected, onDisconnected, onReconnecting } = options;
  const connectionRef = useRef<HubConnection | null>(null);
  const [connectionState, setConnectionState] = useState<HubConnectionState>(HubConnectionState.Disconnected);

  useEffect(() => {
    // Skip the connection while NextAuth (or any other auth source) is still
    // hydrating, OR when caller explicitly passes a falsy token. Without this
    // SignalR negotiates without a token and the server returns 401 in a tight
    // loop until session.update fires.
    if (!accessToken) {
      return;
    }
    const connection = buildConnection(hubUrl, accessToken, autoReconnect);
    connectionRef.current = connection;
    wireLifecycle(connection, setConnectionState, { onConnected, onDisconnected, onReconnecting });

    void connection.start()
      .then(() => {
        setConnectionState(HubConnectionState.Connected);
        onConnected?.();
      })
      .catch(() => {
        // Initial connection failed (e.g., hub not yet authenticated); reconnect handles retry
      });

    return () => {
      void connection.stop();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hubUrl, accessToken]);

  const on = useCallback((event: string, callback: (...args: unknown[]) => void) => {
    connectionRef.current?.on(event, callback);
  }, []);

  const off = useCallback((event: string, callback: (...args: unknown[]) => void) => {
    connectionRef.current?.off(event, callback);
  }, []);

  const invoke = useCallback(async (method: string, ...args: unknown[]) => {
    if (connectionRef.current?.state === HubConnectionState.Connected) {
      await connectionRef.current.invoke(method, ...args);
    }
  }, []);

  return {
    connection: connectionRef.current,
    connectionState,
    on,
    off,
    invoke,
  };
}
