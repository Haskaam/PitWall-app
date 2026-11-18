<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Services\TelemetryService;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

class TelemetryController extends Controller
{
    private TelemetryService $telemetryService;

    public function __construct(TelemetryService $telemetryService)
    {
        $this->telemetryService = $telemetryService;
    }

    public function store(Request $request): JsonResponse
    {
        $expectedToken = (string) config('services.pitwall.token');
        $providedToken = (string) $request->bearerToken();

        if ($expectedToken === '' || !hash_equals($expectedToken, $providedToken)) {
            return response()->json(['message' => 'Unauthorized'], 401);
        }

            $request->validate([
            'schemaVersion' => ['required', 'integer', 'in:1'],
            'timestampUtc' => ['required', 'date'],
            'connected' => ['required', 'boolean'],
            'session' => ['required', 'array'],
            'player' => ['required', 'array'],
            'drivers' => ['required', 'array'],
            'sessionInfo' => ['sometimes', 'nullable', 'array'],
        ]);

        $snapshot = $request->json()->all();


        $this->telemetryService->saveSnapshot($snapshot);

        return response()->json([
            'ok' => true,
        ]);
    }

    public function show(): JsonResponse
    {
        $stored = $this->telemetryService->getLatest();
        if ($stored === null) {
            return response()->json(['message' => 'No telemetry received yet'], 404);
        }

        $ageSeconds = max(0, now()->diffInSeconds($stored['receivedAtUtc']));
        return response()->json([
            'fresh' => $ageSeconds <= 5,
            'ageSeconds' => $ageSeconds,
            'receivedAtUtc' => $stored['receivedAtUtc'],
            'snapshot' => $stored['snapshot'],
        ]);
    }
}
