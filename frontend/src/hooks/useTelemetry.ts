
import { useEffect, useState } from "react";
import type { TelemetryResponse } from "../types/telemetry";

export function useTelemetry() {
    const [telemetry, setTelemetry] =
        useState<TelemetryResponse | null>(null);

    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const controller = new AbortController();
        let active = true;

        async function updateTelemetry() {
            try {
                const response = await fetch("/api/telemetry", {
                    signal: controller.signal,
                });

                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}`);
                }

                const data: TelemetryResponse = await response.json();

                if (active) {
                    setTelemetry(data);
                    setError(null);
                }
            } catch (err) {
                if (!active) return;

                setError(
                    err instanceof Error ? err.message : "Unknown error"
                );
            }
        }

        async function poll() {
            while (active) {
                await updateTelemetry();

                if (!active) break;

                await new Promise<void>((resolve) => {
                    const timeout = window.setTimeout(resolve, 1000);
                    controller.signal.addEventListener(
                        "abort",
                        () => {
                            window.clearTimeout(timeout);
                            resolve();
                        },
                        { once: true }
                    );
                });
            }
        }

        void poll();

        return () => {
            active = false;
            controller.abort();
        };
    }, []);

    return { telemetry, error };
}
