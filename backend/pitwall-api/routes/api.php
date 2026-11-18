<?php

use App\Http\Controllers\Api\TelemetryController;
use App\Http\Controllers\Api\SessionController;
use App\Http\Controllers\Api\FuelStrategyController;
use Illuminate\Support\Facades\Route;

Route::post('/telemetry', [TelemetryController::class, 'store']);
Route::get('/telemetry', [TelemetryController::class, 'show']);
Route::get('/session', [SessionController::class, 'show']);
Route::get('/strategy/fuel', [FuelStrategyController::class, 'show']);

