# Implementing Custom Workflows with DurableOrchestrator

## Overview
This guide offers step-by-step instructions for anyone interested in leveraging the DurableOrchestrator framework to develop and implement custom workflows, enhancing their applications with powerful, scalable, and efficient processing capabilities. Leverage Azure Durable Functions' flexibility, scalability, and comprehensive integration capabilities to enhance your applications.

## Prerequisites
- Azure account and subscription
- Basic knowledge of Azure Functions and Durable Functions
- Familiarity with .NET 8.0 and C#

## High Level Flow

In this sample we focus on how to orchestrate and monitor complex workflows, we selected our trigger to be an HTTP request, this is used for simplicity. 
In each workflow, the following components are used:

- The main workflow class, which inherits from `BaseWorkflow` and orchestrates the workflow. There are two main methods in the workflow:
  - `RunOrchestrator`: This method is the entry point for the workflow and is responsible for orchestrating the activities. This is your main workflow logic, it can call other functions, use any of the dependencies injected during the startup. When calling another Activity function, `context.CallActivityAsync` is used. [Educate yourself](https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-overview?tabs=in-process%2Cnodejs-v3%2Cv1-model&pivots=csharp) on the basic concepts of Azure Durables Functions such as fan-in/fan-out patterns, chaining, and error handling.
  - `HttpStart`: This method is used to trigger the workflow via an HTTP request.
- We included few sample classes to demonstrate how activity functions are created. The main idea is to keep each individual function as simple as possible, stateless and reusable. Use the `BaseActivity` class to ensure consistent telemetry and tracing across all activities.
- Each specific activity function(s) class would also need an extension class, this class is used to instantiate the activity and register it with the dependency injection container. This is important to ensure that the activity function can be injected with any dependencies it needs.

## Setting Up Your Azure Environment

- TBD - should we point to the deployment guide?
- Install .NET 8.0 and the Azure Functions Core Tools locally.

## Integrating DurableOrchestrator into Your Project

- Clone the `DurableOrchestrator` repository.

## Defining Your Workflow

### Enhancing an Existing Workflow

Lets revisit an existing workflow, `TextAnalyticsWorkflow`, and enhance it with addtional activities. As of now the workflow receives a list of product feedback as part of the payload. It calls the `TextAnalyticsActivities` to perform sentiment analysis on the feedback. The call uses a fan-out/fan-in pattern to process the feedback in parallel. Upon sentiment completion the workflows saves the results to a `json` file.

The new requirement is to create an action plan for the product feedback. You should use Azure OpenAI chat completion to generate a markdown file with an action plan. Here is a sample prompt: ```you are a product owner, you get a list of product feedback and the calculated sentiment. and you create an action plan to address the main issues. you respond in a markdown file that has the original product feedback, divided to categories, the next section you suggest to address the feedback.```

Lets review the missing components in the workflow:
- First, we would need an Azure OpenAI chat completion end-point to call.
- We would need to assign proper role to the DurableOrchestrator allowing it to make calls to the OpenAI end-point.
- A new environment variable to store the OpenAI end-point.
- Now we would need to define the extention, and ensure its instantiated and registered with the dependency injection container. You can refer to the `TextAnalyticsExtensions` class for an example, it create the `TextAnalyticClient` that is then used in the activity class. In our case we would need to create a new extension class, `OpenAIExtensions` that would create the `OpenAIClient` and enable registeration it with the dependency injection container. (In `Program.cs`)
- Now after we have the extention class, its time to create the activity class, `OpenAIActivities`. This calss when instantiated would be injected with the `OpenAIClient` and the next thing is to create the method that would use the prompt together with the previous step output (it is a `json` file with the list of product feedback and the calculated sentiment) and call the OpenAI end-point. The result would be a markdown file with the action plan.
- The next step is to write this content to blob.

### Creating a New Workflow

Before you start, you should have a clear understanding of the workflow you want to implement. You should also have a list of activities that the workflow will perform. Address missing Activities and Extensions as they are needed. Follow similar steps in the existing workflow classes. The naming convention is important for the monitoring to be able and pick up your new workflow and activities.

The development can be done either locally or using GitHub codespaces.

## Contributing
We welcome contributions from the community. Please submit pull requests or issues to our GitHub repository to help improve the `DurableOrchestrator` project. We are mainly interested in adding new activities.

