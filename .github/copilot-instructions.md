# Copilot Instructions for Sacks AI Platform

<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

## Project Contract Reference

This project follows the contract established in `PROJECT_CONTRACT.md`. Please adhere to all specifications outlined in that document.

## Key Instructions

### Communication
- Always address the client as "Dear Mr Strul"

### Architecture Requirements
- Implement 5-layer architecture: OsKLayer, InfrastructuresLayer, DataLayer, LogicLayer, GuiLayer
- Maintain strict separation of concerns between layers
- Follow SOLID principles and clean architecture patterns
- Use dependency injection for loose coupling
- Target .NET 9+ for cross-platform compatibility

### Code Standards
- Follow C# coding conventions and best practices
- Use PascalCase naming conventions as per C# standards
- Implement async/await patterns where appropriate
- Use Serilog for structured logging
- Use standard libraries for error handling and logging
- Implement configuration management (appsettings.json)

### Technical Stack
- Excel processing: reading/writing .xlsx files
- HTTP client for internet access
- AI integration: OpenAI/Copilot APIs
- Database abstraction: support AirTable and MSSQL at minimum
- Modular plugin architecture
- Configuration-driven behavior

### Development Guidelines
- Create interfaces for abstractions only when specifically requested
- Add XML documentation only after testing phase
- Create unit tests only after development is complete
- Create README only before deployment
- Never change untracked code without specific permission
- Always ask permission before changing untracked code
- **ALWAYS ask permission before any file manipulation** (rename, move, delete, etc.)
- Never perform file system operations without explicit approval
- Assume valid data only at top level (validate on all other levels)
- Avoid overly complex solutions for simple problems
- Don't implement features not explicitly requested

### Development Mode
- **DEVELOPMENT MODE**: No migrations or backward compatibility needed
- Database schema changes can be made directly without migration considerations
- Feel free to recreate/reset database as needed during development
- Focus on rapid iteration and feature development over data preservation

### Project Structure
- Create separate class library projects for each layer
- Use proper project references and dependencies
- Ensure application is fast, small, and modular

### Communication Protocol
- Confirm architectural decisions before implementation
- Ask for clarification on ambiguous requirements
- Notify about any discovered constraints or issues
- Provide complete, compilable code
- Include setup instructions only if asked
- Explain testing/running only if asked
- Document any assumptions made

## Success Criteria
- Application compiles and runs without errors
- All layers properly separated and testable
- Excel processing functional
- Internet connectivity working
- AI capabilities functional
- Database operations work through abstraction
- Code follows all specified patterns and standards
- Application is fast, small, and modular
