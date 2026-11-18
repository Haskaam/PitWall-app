<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Services\FuelStrategyService;
use Illuminate\Http\JsonResponse;

class FuelStrategyController extends Controller
{
    public function __construct(
        private FuelStrategyService $fuelStrategyService
    ) {}

    public function show(): JsonResponse
    {
        return response()->json(
            $this->fuelStrategyService->calculate()
        );
    }
}
