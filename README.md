# DurableOrchestrator 

The `DurableOrchestrator` project aims to address complex data management and event processing challenges by leveraging the power of Azure Durable Functions. This choice is rooted in our comprehensive exploration of data ingestion frameworks, event processing paradigms, and the quest for a scalable, flexible solution that accommodates a wide array of data types and processing workflows.

**Prescriptive Code vs. No-Code Canvas Based Approach:**

- **Control and Customization:** Offers greater control over the logic and execution flow, essential for handling diverse data sources and complex logic.
- **Debugging and Maintenance:** Enhances debugging capabilities and simplifies maintenance, leveraging familiar development tools and practices.
- **Integration and Extensibility:** Allows for easier integration with existing systems and extending functionality to meet new requirements.

## Key Decisions and Rationale

**Choosing Azure Durable Functions:**

- **Simplicity and Integration:** Provides a simplified development model with seamless integration with other Azure services.
- **Scalability and Performance:** Offers auto-scaling capabilities and robust performance metrics.
- **Stateful Orchestrations:** Facilitates complex, long-running processes in a serverless environment.

**Utilizing the Isolated Pattern:**

- **Dependency Injection:** Supports dynamic incorporation of dependencies for cleaner code and improved modularity.
- **Inheritance and Code Reusability:** Leverages object-oriented features for code reuse and a DRY coding environment.
- **.NET 8.0 Features:** Takes full advantage of .NET 8.0 for enhanced performance, security, and language features.
- **Scalability and Performance:** Aligns with the scalable nature of Azure Durable Functions, ensuring efficient handling of varying loads.

## Conclusion

The `DurableOrchestrator` project demonstrates our strategic decision to use Azure Durable Functions and the isolated pattern, leveraging prescriptive code for a stateful, scalable, and customizable orchestration layer. This approach is chosen over no-code platforms for its control, customization, and integration capabilities, driven by our goal to address the unique challenges of data management and processing workflows.