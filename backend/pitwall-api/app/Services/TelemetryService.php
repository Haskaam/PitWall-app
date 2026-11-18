<?php

namespace App\Services;

use Illuminate\Support\Facades\Cache;

class TelemetryService
{
    private const CACHE_KEY = 'pitwall:latest-snapshot';

    public function getLatest(): ?array
    {
        return Cache::get(self::CACHE_KEY);
    }

    public function saveSnapshot(array $snapshot): void
    {
        Cache::put(self::CACHE_KEY, [
            'receivedAtUtc' => now()->toIso8601String(),
            'snapshot' => $snapshot,
        ], now()->addMinutes(30));
    }
}
