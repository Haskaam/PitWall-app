# SimMetric

**SimMetric** is a web-based live timing and telemetry dashboard for iRacing, built as a personal portfolio project.

The main goal of this project is to create a motorsport-inspired dashboard that displays racing data in a clear and accessible way, while developing my skills in **React, TypeScript, and frontend development**.

The application focuses on real-time race monitoring, including driver positions, lap times, class standings, and pit lane activity.

## Technologies

**Frontend**

- React
- TypeScript
- Vite
- CSS

**Backend**

- PHP
- Laravel

**Telemetry Connector**

- C#
- .NET
- iRacing Telemetry SDK

## Features

### Live Timing

- Real-time driver positions and standings
- Multiclass support
- Drivers grouped by car class
- Gap and interval information
- Last lap and best lap times
- Sorting by position, last lap time, and best lap time
- Class filtering

### Lap Time Highlighting

Lap times are highlighted using colors commonly found in motorsport timing systems:

- **Purple** — fastest lap in the driver's class
- **Green** — new personal best lap
- **Default** — regular lap time

The `LAST` column tracks new lap results and retains their assigned colors until the next lap is completed.

The `BEST` column highlights the fastest lap within each car class.

### Pit Lane Monitoring

- Detects when a driver enters the pit lane
- Displays the current pit lane duration
- Uses a blue pulsing animation while a driver is in the pit lane
- Displays the last recorded pit lane duration after the driver exits

### Dashboard

- Session information
- Track and air temperature
- Dedicated dashboard sections for timing, fuel, and race monitoring

## How It Works

Pit Wall consists of three main parts:

**1. Telemetry Connector (C# / .NET)**

The connector reads telemetry data from iRacing and prepares it for the backend.

**2. Backend (Laravel)**

The backend acts as a communication layer between the telemetry connector and the frontend.

**3. Frontend (React / TypeScript)**

The frontend receives telemetry snapshots and displays them in an interactive racing dashboard&#x20;

## Project Purpose

Pit Wall is primarily a learning and portfolio project.

I started this project to gain practical experience with React and TypeScript by building something related to one of my interests: simracing and motorsport.

Rather than focusing only on tutorials or small isolated exercises, I wanted to work on an application that involves real-time data, component-based architecture, state management, conditional rendering, and interactive UI elements.

The project is still under development, and some features may be incomplete or behave differently depending on the available iRacing telemetry data.

## AI Assistance

AI tools have been used during the development of this project.

**The C#/.NET telemetry connector was generated with AI assistance and is not code I originally wrote myself.** It serves as the data source for the application, allowing me to focus primarily on frontend development.

I also use AI as a learning and development assistant, including for:

- Understanding React and TypeScript concepts
- Discussing implementation approaches
- Debugging and reviewing code
- Improving existing functionality
- Learning better development practices

The React frontend is the main part of the project where I actively practice implementing features, understanding the code, and developing my programming skills.

I believe being transparent about AI usage is important, especially when presenting a project as part of a developer portfolio.

## Screenshots

*Screenshots of the dashboard will be added later.*

## Project Status

**Work in progress**

Pit Wall is an ongoing personal project. Additional features, UI improvements, and code refactoring are planned as I continue learning React and TypeScript.

## Disclaimer

Pit Wall is an unofficial personal project and is not affiliated with or endorsed by iRacing.
