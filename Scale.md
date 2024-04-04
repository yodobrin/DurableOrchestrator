# Durable Orchestrator - Scale and Performance

## Introduction

Our deployment approach for the `DurableOrchestrator` project utilizes Azure Container App as the hosting platform. This selection enables horizontal scaling by adding more instances of the containerized application to accommodate varying loads. While our current strategy packages all workflows into a single container, we acknowledge this may not be the optimal solution for every scenario. One key objective of our testing is to identify the limitations and constraints inherent to this approach, with a focus on providing actionable deployment considerations, including scale metrics and optimal pod sizing (CPU/memory).

## Scale Goals & Testing

As the `DurableOrchestrator` project is designed to orchestrate multiple types of workflows, it needs to be able to handle increased load, and also scaling down when the load decreases. Given that workflows can be triggered by various events we would use `Event Hub` as our designated trigger. The main reason here is the ease of creating load. The scaling in Azure Container App is based on [Scaling Rules](https://learn.microsoft.com/en-us/azure/container-apps/scale-app?pivots=azure-portal).

### Objectives

- **Scalability:** Ensure the `DurableOrchestrator` can efficiently scale up to handle peak loads and scale down during periods of low activity without manual intervention.
- **Performance:** Maintain optimal performance metrics, such as response time and throughput, under varying loads.
- **Resource Optimization:** Evaluate resource utilization to guide the configuration of pod sizes and scale parameters, aiming for cost-effectiveness without compromising on performance.

### Testing Strategy

- **Load Testing:** Simulate real-world traffic patterns to understand how the system behaves under normal and peak conditions. This includes sudden spikes and gradual increases in load.
- **Stress Testing:** Determine the limits of our system by gradually increasing the load until the system fails to process additional requests. This helps identify the maximum capacity our application can handle.
- **Longevity Testing:** Run the system under a significant load for a prolonged period to identify potential issues with resource leakage, degradation of performance over time, and the system's ability to recover from failures. (stretch goal)

### Test Environment

TBD, explain how the test env would be created, what resources, number of files, and other settings.

### Test Scenarios

We will test two workflows:

- Json2Parquet: This workflow will convert the content of a folder/container from JSON (lines) to Parquet format, it will retain the same folder structure. This workflow would have a configurable fan-out parameter.
- InvoiceProcessing: This workflow will process invoices, extract relevant information, and store it in blob storage as json. The workflow would be executing several activity calls in sequence, calling Azure Document Intelligence and Azure OpenAI.

We would perform the following tests: (initial draft)

| Scenario | Replica Count | `json2parquet` Fan-Out | `json2parquet` Trigger Rate | `invoiceProcessing` Trigger Rate |
|----------|---------------|------------------------|-----------------------------|----------------------------------|
| Baseline | 1             | 1 to 10                | 1 per minute                | 10 per minute                    |
| Scenario 1 | 5           | 1 to 50                | 5 per minute                | 50 per minute                    |
| Scenario 2 | 10          | 1 to 100               | 10 per minute                | 100 per minute                    |
| Scenario 3 | 15          | 1 to 150               | 15 per minute                | 150 per minute                    |


During the test, we will monitor the following metrics: CPU Usage, Requests, Memory Working set for Azure Container App. Application specific monitoring (success & failure rates would also be logged - we aim for 100% success)

### Test Results







