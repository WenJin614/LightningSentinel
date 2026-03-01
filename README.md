# Lightning Node Health Monitor Dashboard

![Node Health Monitor Screenshot](image_0.png)

This project provides a real-time, visual dashboard built with Blazor that monitors the health and reliability of a Lightning Network node. It leverages a backend API to calculate a dynamic "Health Score" based on a series of recent probe results.

## Key Features Visualized in the Screenshot

* **Overall Score**: A quick, color-coded visual indicator of the node's health is always visible in the top right. A perfect 100% score is shown here.
* **Key Health Metrics**: The top of the dashboard provides high-level metrics derived from the last set of probes. This includes an aggregate 100% reliability rating, an 862 ms average latency across all checks, and a perfect 9/9 uptime count for the most recent probe attempts.
* **Detailed Probe History**: A clean, chronological list shows the individual results of each probe attempt. This includes:
    * **Timestamp**: The exact time of the probe.
    * **Status**: A clear "ONLINE" (green) indicator, demonstrating the node was successfully reached.
    * **Latency**: The precise round-trip time in milliseconds for each successful probe, which can be seen to fluctuate, sometimes dramatically as evidenced by the 5760 ms value.

## How It Works

1.  **Backend Probing**: The system is designed with a background service that can perform periodic "probes" (e.g., using gRPC calls like `GrpcProbeService`).
2.  **Data Storage**: The `ProbeService` then stores each probe's outcome (`PubKey`, `IsAlive` status, and `LatencyMs`) into a persistent PostgreSQL database using Entity Framework Core.
3.  **Calculated Metrics API**: The `ProbesController` exposes an HTTP GET endpoint (`api/v1/probes/{pubKey}`) that retrieves recent data. This controller is also responsible for calculating the aggregate health score and other key metrics directly from the raw data.
4.  **Blazor Frontend**: The user interface is a Blazor application running in **InteractiveServer** mode.
5.  **Clean API Interaction**: To keep the UI code clean and robust, the dashboard uses a typed HTTP client called `ProbeHttpClient` (which includes necessary URL encoding to handle public key strings safely). This client makes the API call to the backend.
6.  **Dynamic Rendering**: When the `/node-health/{PubKey}` page is loaded, it asynchronously fetches the data via the `ProbeHttpClient` and then dynamically renders the statistics and history table seen in the screenshot. The colors of the health score and the status badges adjust automatically based on the results.

### Summary of Component Roles

| Component | Responsibility |
| :--- | :--- |
| `SentinelDbContext` | Defiens the PostgreSQL database schema and acts as the data access layer. |
| `ProbeService` | Implements the core business logic for both saving and retrieving probe results. |
| `ProbesController` | Handles HTTP requests, calls the service layer, and calculates summary metrics like the health score. |
| `ProbeHttpClient` (Blazor) | Encapsulates the communication with the API from the frontend's perspective. |
| `HealthDashboard.razor` (Blazor) | The UI component that presents the data visually, providing the dashboard interface seen above. |