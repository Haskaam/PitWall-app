<?php

namespace App\Services;

class FuelStrategyService
{
    public function __construct(
        private TelemetryService $telemetryService
    ) {}

    public function calculate(): array
    {
        $stored = $this->telemetryService->getLatest();

        if ($stored === null) {
            return [
                'available' => false,
                'reason' => 'No telemetry received',
            ];
        }

        $snapshot = $stored['snapshot'] ?? [];
        $player = $snapshot['player'] ?? [];
        $session = $snapshot['session'] ?? [];
        $drivers = $snapshot['drivers'] ?? [];

        $currentFuel = $player['fuelLevelLiters'] ?? null;
        $fuelData = $player['fuel'] ?? [];

        $fuelPerLap = $fuelData['averageLast10LapsLiters']
            ?? $fuelData['averageLast3LapsLiters']
            ?? null;

        $lapsToEmpty = $fuelData['lapsToEmpty'] ?? null;
        $timeRemaining = $session['sessionTimeRemainSeconds'] ?? null;

        $playerCarIdx = $player['carIdx'] ?? null;
        $averageLapTime = null;

        foreach ($drivers as $driver) {
            if (($driver['carIdx'] ?? null) === $playerCarIdx) {
                $averageLapTime = $driver['averageLast3Laps'] ?? null;
                break;
            }
        }

        $fuelValid = is_numeric($currentFuel)
            && is_numeric($fuelPerLap)
            && $currentFuel >= 0
            && $fuelPerLap > 0;

        $paceValid = is_numeric($averageLapTime)
            && $averageLapTime > 0;

        $timeValid = is_numeric($timeRemaining)
            && $timeRemaining >= 0
            && $timeRemaining < 1000000;

        if (!$fuelValid) {
            return [
                'available' => false,
                'reason' => 'Waiting for valid fuel samples',
                'currentFuelLiters' => $currentFuel,
                'validLapSamples' => $fuelData['validLapSamples'] ?? 0,
            ];
        }

        $currentFuel = (float) $currentFuel;
        $fuelPerLap = (float) $fuelPerLap;

        $lapsToEmpty = is_numeric($lapsToEmpty)
            ? (float) $lapsToEmpty
            : $currentFuel / $fuelPerLap;

        $result = [
            'available' => true,
            'currentFuelLiters' => round($currentFuel, 2),
            'fuelPerLapLiters' => round($fuelPerLap, 3),
            'lapsToEmpty' => round($lapsToEmpty, 2),
            'validLapSamples' => $fuelData['validLapSamples'] ?? 0,
            'estimatedLapsRemaining' => null,
            'fuelNeededLiters' => null,
            'fuelBalanceLiters' => null,
            'canFinish' => null,
        ];

        if (!$paceValid || !$timeValid) {
            return $result;
        }

        $estimatedLapsRemaining =
            (float) $timeRemaining / (float) $averageLapTime;

        $fuelNeeded = $estimatedLapsRemaining * $fuelPerLap;
        $fuelBalance = $currentFuel - $fuelNeeded;

        $result['estimatedLapsRemaining'] =
            round($estimatedLapsRemaining, 2);

        $result['fuelNeededLiters'] = round($fuelNeeded, 2);
        $result['fuelBalanceLiters'] = round($fuelBalance, 2);
        $result['canFinish'] = $fuelBalance >= 0;

        return $result;
    }
}
