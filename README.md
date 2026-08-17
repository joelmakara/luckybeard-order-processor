# Technical Assessment: .NET Refactoring & Architecture Challenge

This repository holds my solution to Lucky Beard's .NET refactoring assessment.
The brief below is copied verbatim from the assessment page (an external
Confluence share), since shared links can expire. The starter code it refers to
is preserved unmodified in the baseline commit.

---

## Introduction to the file

You have inherited a legacy Order Processor Flow that has become unwieldy and hard to maintain.

Identify issues, improve upon the file.

This is not a real flow, so do not worry too much about actual flows running end-to-end.

Please add mock endpoints to the flow.

Make use of .net 8-10 concepts.

## 1. Overview

You have inherited a legacy Order Processor Flow that has become unwieldy and hard to maintain.

Your task is to review, analyse, and refactor the component into a solution that is:

- Scalable
- Maintainable
- Performance
- Well-structured

The goal is not to shorten the code, but to improve its architecture, separation of concerns, and long-term maintainability.

We are interested in how the candidate thinks about:

- Architectural problems
- Code security
- .NET standards and Best practices
- Single Responsibility and good Locality of Behavior Thinking
- Interface segregation
- Decoupling
- Developer experience and maintainability thinking.

## 2. What We Would Like to See

What are some specific things we want to see in the solution.

- Strategy Pattern [bonus]
- Dependency Injection
- Validation of Data
- Logging (preferably wide)
- Good Error Handling (coupled with good logging)
- DB Transactions
- No hard-coded Secrets, magic strings or magic numbers

## 3. Key Known Issues

- SQL Injection Vulnerability
- Hard-coded DB Credentials
- This code is a god-class that handles everything in the whole flow
- Bad/No exception handling
- Method/Variable naming
- Memory leaks (not disposing of connections)
- No dependency injection/tight coupling
