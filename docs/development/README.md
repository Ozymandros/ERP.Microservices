# 👨‍💻 Development Guidelines

**Setup, Coding Standards, Testing, Git Workflow & Debugging**  
Last Updated: October 27, 2025

---

## 📍 Overview

This category covers everything a developer needs to get productive. From local setup to coding standards, testing practices, Git workflows, and debugging techniques. Designed for all developers on the team.

---

## 📚 Documents in This Category

### [DEVELOPMENT_SETUP.md](DEVELOPMENT_SETUP.md)
**Local development environment setup**
- Prerequisites and installation
- Repository cloning
- Dependencies and tools
- First service startup
- Verification steps

### [CODING_STANDARDS.md](CODING_STANDARDS.md)
**Code style and conventions**
- Naming conventions
- C# style guidelines
- Project organization
- File structure
- Documentation requirements

### [TESTING.md](TESTING.md)
**Testing practices and frameworks**
- Unit testing with xUnit
- Integration testing
- Test organization
- Test data setup
- Coverage requirements

### [GIT_WORKFLOW.md](GIT_WORKFLOW.md)
**Git branching and commit practices**
- Branch naming
- Commit message format
- Pull request process
- Code review guidelines
- Merge strategies

### [DEBUGGING.md](DEBUGGING.md)
**Debugging techniques and tools**
- Visual Studio debugging
- Remote debugging
- Log analysis
- Common issues
- Performance profiling

### [DEVCONTAINER_SETUP.md](DEVCONTAINER_SETUP.md)
**DevContainer configuration for local and cloud development**
- Local development setup (host Docker)
- GitHub Codespaces setup (Docker-in-Docker)
- VS Code configuration
- Troubleshooting guide

---

## 🎯 Developer Journey

### Day 1: Setup
1. Read [DEVELOPMENT_SETUP.md](DEVELOPMENT_SETUP.md)
2. Follow setup steps
3. Get first service running
4. Verify with health check

### Day 2: Code
1. Read [CODING_STANDARDS.md](CODING_STANDARDS.md)
2. Create feature branch
3. Make code changes
4. Run tests locally

### Day 3: Testing & Review
1. Write unit tests
2. Run all tests locally
3. Create pull request
4. Follow [GIT_WORKFLOW.md](GIT_WORKFLOW.md)

### Day 4+: Debug & Deploy
1. Review feedback
2. Use [DEBUGGING.md](DEBUGGING.md) for issues
3. Merge and deploy
4. Monitor in production

---

## 📊 Quick Reference

| Task | Time | See Also |
|------|------|----------|
| First-time setup | 30 min | [DEVELOPMENT_SETUP.md](DEVELOPMENT_SETUP.md) |
| Create new feature | 15 min | [GIT_WORKFLOW.md](GIT_WORKFLOW.md) |
| Write unit test | 10 min | [TESTING.md](TESTING.md) |
| Debug issue | 15 min | [DEBUGGING.md](DEBUGGING.md) |
| Code review | 20 min | [CODING_STANDARDS.md](CODING_STANDARDS.md) |
| Commit changes | 5 min | [GIT_WORKFLOW.md](GIT_WORKFLOW.md) |

---

## 🔧 Developer Toolbox

### Required Tools
- Visual Studio 2022 or VS Code
- .NET SDK 9.0+
- Docker Desktop
- Git
- Azure CLI

### Recommended Tools
- Postman or Insomnia (API testing)
- Azure Data Studio (database)
- Redis Commander (cache)
- Application Insights (monitoring)

---

## 🔄 Development Workflow

```
1. Pull latest code
   ↓
2. Create feature branch (git workflow)
   ↓
3. Make code changes (coding standards)
   ↓
4. Run tests locally (testing guide)
   ↓
5. Test in Docker Compose
   ↓
6. Commit with good messages (git workflow)
   ↓
7. Push and create PR
   ↓
8. Code review
   ↓
9. Address feedback
   ↓
10. Merge to main
    ↓
11. CI/CD pipeline runs
    ↓
12. Deployed to staging
    ↓
13. Manual testing
    ↓
14. Deployed to production
```

---

## 📚 Related Categories

- **Architecture:** [Architecture Guide](../architecture/README.md) - Understand design
- **Microservices:** [Microservices Guide](../microservices/README.md) - Service patterns
- **Docker:** [Docker Compose](../docker-compose/README.md) - Local environment
- **Security:** [Security Guide](../security/README.md) - Secure coding

---

## 🔄 Reading Order

1. Start with [DEVELOPMENT_SETUP.md](DEVELOPMENT_SETUP.md) on day 1
2. Read [CODING_STANDARDS.md](CODING_STANDARDS.md) before coding
3. Reference [TESTING.md](TESTING.md) when writing tests
4. Follow [GIT_WORKFLOW.md](GIT_WORKFLOW.md) for commits
5. Bookmark [DEBUGGING.md](DEBUGGING.md) for troubleshooting

---

## ✅ Developer Checklist

### When Starting
- [ ] Environment set up correctly
- [ ] Can run services locally
- [ ] Can access databases
- [ ] Health checks passing

### Before Coding
- [ ] Read coding standards
- [ ] Understand service architecture
- [ ] Know database schema
- [ ] Have test database ready

### Before Committing
- [ ] Code follows standards
- [ ] Unit tests written
- [ ] All tests passing
- [ ] No console errors
- [ ] Commit message clear
- [ ] Branch name correct

### Before Creating PR
- [ ] Latest main merged
- [ ] Integration tests pass
- [ ] Manual testing done
- [ ] Documentation updated
- [ ] No sensitive data committed

---

## 💡 Development Best Practices

### Code Quality
- Write testable code
- Keep functions small
- Use meaningful names
- Add helpful comments
- Follow SOLID principles

### Testing
- Write tests first (TDD)
- Test edge cases
- Aim for > 80% coverage
- Integration tests matter
- Mock external dependencies

### Git Discipline
- Commit frequently
- Clear commit messages
- Small, focused PRs
- Timely reviews
- No force pushes to main

### Performance
- Profile before optimizing
- Use caching strategically
- Lazy load when possible
- Batch operations
- Monitor in production

### Security
- Never commit secrets
- Validate all inputs
- Use parameterized queries
- Log securely
- Check dependencies

---

## 🆘 Common Developer Issues

| Issue | Solution |
|-------|----------|
| Can't run Docker | [DEVELOPMENT_SETUP.md](DEVELOPMENT_SETUP.md) |
| Tests failing | [TESTING.md](TESTING.md) |
| Code style questions | [CODING_STANDARDS.md](CODING_STANDARDS.md) |
| Git confuse | [GIT_WORKFLOW.md](GIT_WORKFLOW.md) |
| Debugging service | [DEBUGGING.md](DEBUGGING.md) |

---

## 📊 Team Metrics

| Metric | Target | Status |
|--------|--------|--------|
| **Test Coverage** | > 80% | ✅ |
| **Code Review Time** | < 24 hours | ✅ |
| **Build Time** | < 5 min | ✅ |
| **Test Suite Time** | < 3 min | ✅ |
| **Deploy Time** | < 10 min | ✅ |

---

## 📞 Next Steps

- **New to team?** → [DEVELOPMENT_SETUP.md](DEVELOPMENT_SETUP.md)
- **Starting to code?** → [CODING_STANDARDS.md](CODING_STANDARDS.md)
- **Writing tests?** → [TESTING.md](TESTING.md)
- **Submitting PR?** → [GIT_WORKFLOW.md](GIT_WORKFLOW.md)
- **Debugging?** → [DEBUGGING.md](DEBUGGING.md)

---

## 🔗 Full Document Map

```
development/
├── README.md (this file)
├── DEVELOPMENT_SETUP.md
├── CODING_STANDARDS.md
├── TESTING.md
├── GIT_WORKFLOW.md
├── DEBUGGING.md
└── DEVCONTAINER_SETUP.md
```

---

**Last Updated:** January 28, 2026  
**Category Status:** ✅ Complete  
**Documents:** 6 files  
**Target Coverage:** > 80%
