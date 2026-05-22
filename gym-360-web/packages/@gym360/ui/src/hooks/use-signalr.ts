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
    const builder = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: accessToken ? () => accessToken : undefined,
      })
      .configureLogging(LogLevel.Warning);

    if (autoReconnect) {
      builder.withAutomaticReconnect([0, 2000, 5000, 10000, 30000]);
    }

    const connection = builder.build();
    connectionRef.current = connection;

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
