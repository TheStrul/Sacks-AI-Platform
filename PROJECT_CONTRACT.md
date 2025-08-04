# Project Contract: Sacks AI Platform

 - always address me as "Dear Mr Strul"
## Project Overview
A modular, object-oriented C# console application with AI capabilities, Excel processing, internet access, and database abstraction following enterprise architecture patterns.

## What I WILL Do ✅

### Architecture & Design

- Create a 5-layer architecture: OsKLayer, InfrastructuresLayer, DataLayer, LogicLayer, GuiLayer
- Implement strict separation of concerns between layers
- Follow SOLID principles and clean architecture patterns
- Use dependency injection for loose coupling
- Create interfaces for all major components (abstraction) - only if specificaly requested
- Implement proper error handling and logging - using standart libreries
- Design for cross-platform compatibility (.NET 9+)

### Code Standards & Quality
- Follow C# coding conventions and best practices
- Implement async/await patterns where appropriate
- Use proper naming conventions (PascalCase as per C# standards)
- Add comprehensive XML documentation - only after done testing
- Create unit tests for critical components - only after done developing
- Implement configuration management (appsettings.json)
- Use NuGet packages for external dependencies

### Technical Implementation
- Excel file processing capabilities (reading/writing .xlsx)
- HTTP client for internet access
- AI integration (OpenAI/Copilot)
- Database abstraction layer (supporting multiple providers - at least AirTable and MtSql
- Modular plugin architecture
- Comprehensive logging (structured logging) - using serilog
- Configuration-driven behavior

### Project Structure
- Create separate class library projects for each layer
- Use proper project references and dependencies
- Implement proper build and deployment configurations
- Create comprehensive README with setup instructions - only befor diploying

## What I WON'T Do ❌

 - Never  - Never - Never - change code that is not trucked without specific permitions

### Architecture Violations
- Mix concerns between layers
- Create circular dependencies
- Hardcode connections strings or API keys
- Skip interface abstractions
- Create monolithic classes or methods without specificaly asked to
- Ignore performance considerations
 - asume valid data in top level (on all other levels no need to check validity)

### Bad Practices

### Scope Limitations
- Won't create overly complex solutions for simple problems
- Won't implement features not explicitly requested or discussed

## Communication Protocol

### Before Implementation
- Confirm architectural decisions with you
- Ask for clarification on ambiguous requirements

### During Implementation
 - specificaly ask permition befor changind=g untruced code
- Notify about any discovered constraints or issues

### Code Delivery
- Provide complete, compilable code
- Include setup and configuration instructions - only if asked to
- Explain how to test and run the application - only if asked to
- Document any assumptions made

## Success Criteria
- ✅ Application compiles and runs without errors
- ✅ All layers are properly separated and testable
- ✅ Excel files can be processed successfully
- ✅ Internet connectivity works as expected
- ✅ AI capabilities are functional
- ✅ Database operations work through abstraction
- ✅ Code follows all specified patterns and standards
- ✅ Application is fast, small, and modular

## Agreement
By proceeding, we both agree to follow this contract. You can reference this document at any time to clarify expectations or request modifications to our approach.

**Ready to proceed?** Please confirm you agree with this contract, and I'll begin implementing your C# solution following these guidelines.

---
*Created: August 5, 2025*
*Project: Sacks AI Platform*
