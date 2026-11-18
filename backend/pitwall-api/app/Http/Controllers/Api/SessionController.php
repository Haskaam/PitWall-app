<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Services\TelemetryService;
use Illuminate\Http\JsonResponse;

class SessionController extends Controller
{
    private TelemetryService $telemetryService;

    public function __construct(TelemetryService $telemetryService)
    {
        $this->telemetryService = $telemetryService;
    }

    public function show(): JsonResponse
    {
        $stored = $this->telemetryService->getLatest();
        if ($stored === null) {
            return response()->json(['message' => 'Snapshot do not exist'], 503);
        }
        $ageSeconds = max(0, now()->diffInSeconds($stored['receivedAtUtc']));

        $fresh = $ageSeconds <= 5;
        $session = $stored['snapshot']['session'] ?? null;

        return response()->json([
            'fresh' => $fresh,
            'session' => $session,
        ]);
    }
}
