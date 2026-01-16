# 📋 Documentation Conventions & Standards

**Document Format, Standards & Best Practices**  
Last Updated: October 27, 2025

---

## 📐 **Document Structure Template**

All documentation should follow this structure:

### Header Section (Required)
```markdown
# [TITLE]

**Purpose:** One-line description  
**Audience:** Who should read this  
**Prerequisites:** What to know first  
**Last Updated:** Date  
**Status:** ✅ Complete | ⏳ In Progress | 📌 Planned
```

### Navigation Section (Recommended)
```markdown
---

## 📍 Quick Navigation

**Related Documents:**
- [Link 1](path/file.md)
- [Link 2](path/file.md)

**Previous:** [Prev Doc](path/prev.md) | **Next:** [Next Doc](path/next.md)

---
```

### Content Sections
- Overview
- Table of Contents (if > 1000 words)
- Main Content (broken into logical sections)
- Examples
- Troubleshooting
- Related Resources

### Footer Section (Required)
```markdown
---

## 📞 Questions or Issues?

- **Something unclear?** Check related documents above
- **Found an error?** Submit a fix
- **Have a question?** See [FAQ](../reference/GLOSSARY.md)

**Last Updated:** October 27, 2025  
**Maintained By:** [Team Name]  
**Contributing:** [See CONTRIBUTING.md]
```

---

## 🏷️ **Naming Conventions**

### File Names

**Pattern:** `CATEGORY_SUBCATEGORY.md` or `SPECIFIC_TOPIC.md`

**Examples:**
- ✅ `ARCHITECTURE_OVERVIEW.md`
- ✅ `OCELOT_CONFIGURATION.md`
- ✅ `DEVELOPMENT_SETUP.md`
- ❌ `setup.md` (not descriptive enough)
- ❌ `architecture overview.md` (spaces, lowercase)
- ❌ `arch_overview.md` (abbreviated, confusing)

**Special Files:**
- `README.md` - Category overview or main entry point
- `TROUBLESHOOTING.md` - Problem solving guide
- `GLOSSARY.md` - Terms and definitions
- `INDEX.md` - Cross-references and quick links

### Heading Hierarchy

```markdown
# Main Title (H1)
## Major Section (H2)
### Subsection (H3)
#### Detail (H4)
##### Sub-detail (H5)
###### Minimal use (H6)
```

**Rules:**
- Use H1 once at top
- Use H2 for major sections
- Use H3 for subsections
- Don't skip levels (H2 → H4 is wrong)
- Maximum 3 levels deep for readability

---

## 🎨 **Formatting Standards**

### Emphasis

```markdown
**Bold text** - Important terms, button names, file names
*Italic text* - Emphasis, variable names, technical terms
`Code` - Inline code, commands, file paths, variable names
***Bold italic*** - Critical emphasis (rare)
```

**Examples:**
- ✅ Click the **Deploy** button
- ✅ Set the `REDIS_PASSWORD` environment variable
- ✅ The *service* must be running
- ✅ Use `docker compose up` to start

### Code Blocks

**Proper Format:**
````markdown
```language
code here
```
````

**Common Languages:**
- `bash` / `shell` - Terminal/PowerShell commands
- `csharp` / `cs` - C# code
- `json` - JSON configuration
- `yaml` - YAML files
- `sql` - SQL queries
- `http` - HTTP requests
- `xml` - XML files
- `dockerfile` - Docker files
- `bicep` - Bicep templates
- `markdown` - Markdown examples

**Examples:**
```bash
# Good - Command with comment
docker compose up -d
```

```csharp
// Good - C# code with context
public class UserService
{
    public async Task<User> GetUserAsync(int id)
    {
        return await _repository.GetUserAsync(id);
    }
}
```

### Lists

**Unordered (Bullets):**
- Use for: Sets, collections, non-sequential items
- Prefix with `-` or `*`
- Indent with 2 spaces for nesting

**Ordered (Numbered):**
- Use for: Steps, procedures, sequences
- Prefix with `1.`, `2.`, etc.
- Indent with 2 spaces for nesting

**Definition Lists:**
```markdown
**Term:** Definition or explanation
**Another Term:** Its definition
```

---

## 🔗 **Linking Standards**

### Internal Links

**Format:** `[Display Text](relative/path/to/file.md)`

**Examples:**
```markdown
# Good - Relative paths
See [Architecture Overview](../architecture/ARCHITECTURE_OVERVIEW.md)
Read [Security Best Practices](../security/BEST_PRACTICES.md)
Check [Troubleshooting Index](../reference/TROUBLESHOOTING_INDEX.md)

# Good - Anchor links (section references)
See [Authentication](#authentication) section below
Jump to [Installation](#installation)

# Bad - Absolute paths
See /Users/name/docs/file.md
See c:\Projects\file.md

# Bad - External links in same project
See https://github.com/.../docs/file.md
```

### Heading Anchors

Automatic in most renderers. Reference with:
```markdown
[Jump to section](#section-heading)
```

Heading "## My Section" → anchor `#my-section`

### External Links

**Format:** `[Display Text](https://full-url)`

**Examples:**
```markdown
Learn more: [Azure Documentation](https://learn.microsoft.com/azure)
Reference: [DAPR Docs](https://dapr.io/docs/)
Tool: [Docker](https://www.docker.com/)
```

---

## 📊 **Table Standards**

### Basic Table

```markdown
| Header 1 | Header 2 | Header 3 |
|----------|----------|----------|
| Data 1   | Data 2   | Data 3   |
| Data 4   | Data 5   | Data 6   |
```

### Aligned Columns

```markdown
| Left | Center | Right |
|:-----|:------:|------:|
| L    | C      | R     |
```

### Status Table

```markdown
| Item | Status | Notes |
|------|--------|-------|
| Feature A | ✅ Complete | Ready for use |
| Feature B | ⏳ In Progress | Est. 2 weeks |
| Feature C | 📌 Planned | Q4 2025 |
```

---

## 🏳️ **Status Badges**

### Document Status
```markdown
**Status:** ✅ Complete | ⏳ In Progress | 📌 Planned | ⚠️ Outdated
```

### Feature Status
```markdown
- ✅ Implemented
- ⏳ In Progress
- 📌 Planned
- 🔄 Under Review
- ❌ Deprecated
- ⚠️ Needs Update
```

### Compatibility
```markdown
- ✅ .NET 9.0+
- ✅ Docker Desktop 4.0+
- ✅ Azure Container Apps
- ❌ .NET 8.0 (not supported)
```

---

## 💡 **Special Callout Boxes**

### Tip (Optional Information)
```markdown
> 💡 **Tip:** Use this helpful shortcut for faster development.
```

### Note (Important but not critical)
```markdown
> 📝 **Note:** This configuration is environment-specific.
```

### Warning (Important!)
```markdown
> ⚠️ **Warning:** This action cannot be undone!
```

### Critical (Stop and read!)
```markdown
> 🚨 **CRITICAL:** Failure to follow this will cause data loss!
```

### Example
```markdown
> 📌 **Example:** Here's how to do this correctly...
```

### Security
```markdown
> 🔒 **Security:** Never commit secrets to version control.
```

---

## 📝 **Code Examples**

### Minimal Examples
```markdown
Quick example (1-3 lines):
​```bash
command here
​```

**Result:**
Output description
```

### Complete Examples
```markdown
**Full Example:**
​```csharp
// Full implementation with context
public class Example
{
    // Details...
}
​```

**Explanation:**
Line-by-line explanation of important parts
```

### Before/After Examples
```markdown
**Before (Wrong):**
​```csharp
// Incorrect implementation
​```

**After (Correct):**
​```csharp
// Correct implementation
​```

**Key Differences:**
- Point 1
- Point 2
```

---

## 📸 **Diagrams & Visual Aids**

### Mermaid Diagrams

```markdown
​```mermaid
graph TD
    A[Node A] -->|Label| B[Node B]
    B --> C{Decision}
    C -->|Yes| D[Result Yes]
    C -->|No| E[Result No]
​```
```

### Flowcharts
```markdown
​```mermaid
flowchart LR
    Start([Start]) --> Step1[Step 1]
    Step1 --> Step2[Step 2]
    Step2 --> End([End])
​```
```

### Sequence Diagrams
```markdown
​```mermaid
sequenceDiagram
    participant Client
    participant Gateway
    participant Service
    Client->>Gateway: Request
    Gateway->>Service: Forward
    Service-->>Gateway: Response
    Gateway-->>Client: Return
​```
```

### Tables Instead of Images
Use markdown tables for comparisons and data:
```markdown
| Feature | Description | Status |
|---------|-------------|--------|
| Feature A | Does X | ✅ |
| Feature B | Does Y | ⏳ |
```

---

## 📋 **Checklist Template**

### Quick Checklist
```markdown
**Prerequisites:**
- [ ] Item 1 completed
- [ ] Item 2 completed
- [ ] Item 3 completed

**Setup Steps:**
- [ ] Step 1
- [ ] Step 2
- [ ] Step 3

**Verification:**
- [ ] Check A passes
- [ ] Check B passes
- [ ] Check C passes
```

### Procedure Checklist
```markdown
**Before You Start:**
- [ ] Read [Prerequisite Doc](path/file.md)
- [ ] Have [Required Tools](path/tools.md) installed
- [ ] Access to [Resources](path/resources.md)

**Execution:**
1. [ ] First step
2. [ ] Second step
3. [ ] Third step

**Verification:**
- [ ] Verify X
- [ ] Check Y
- [ ] Confirm Z
```

---

## 🎯 **Writing Style Guide**

### Voice & Tone
- **Professional** but friendly
- **Clear** over clever
- **Direct** over verbose
- **Active** voice preferred
- **Second person** (you, your) when addressing reader

### Sentence Structure
- **Short sentences** (avg 15-20 words)
- **Active voice:** "You configure the gateway" vs "The gateway is configured"
- **Imperative:** "Click Deploy" not "You should click Deploy"
- **Present tense:** "The service runs" not "The service will run"

### Word Choice
- **Use:** setup, enter, type, click, run, execute
- **Avoid:** endeavor, facilitate, utilize, paradigm
- **Technical terms:** Explain first time used
- **Acronyms:** Spell out first time, then use (e.g., "Role-Based Access Control (RBAC)")

### Grammar & Punctuation
- Use Oxford comma in lists: "A, B, and C"
- Avoid contractions: "do not" not "don't"
- Use single space after periods
- Capitalize: Proper nouns, product names, UI elements

### Examples

**Good:**
> Run the deployment command. This creates the infrastructure and deploys your services. You should see confirmation messages.

**Better:**
> Run `az deployment create -f main.bicep` to create the infrastructure and deploy your services. Watch for confirmation messages.

**Best:**
> Run the deployment:
> ```bash
> az deployment create -f main.bicep
> ```
> You'll see confirmation when complete.

---

## 🔄 **Related Documents Link Pattern**

Every document should include related links:

```markdown
---

## 🔗 Related Documents

**Read Next:**
- [Next Logical Topic](path/file.md)
- [Common Follow-up](path/file.md)

**Prerequisites:**
- [Needed First](path/file.md)
- [Background Reading](path/file.md)

**Deep Dives:**
- [Advanced Topic](path/file.md)
- [Alternative Approach](path/file.md)

**See Also:**
- [Reference Material](path/file.md)
- [Glossary](../reference/GLOSSARY.md)
```

---

## ✅ **Pre-Publish Checklist**

Before publishing a document:

- [ ] **Spelling & Grammar**
  - [ ] Ran spell checker
  - [ ] Grammar is correct
  - [ ] Consistent terminology

- [ ] **Structure**
  - [ ] Clear header with purpose/audience
  - [ ] Logical sections with H2/H3
  - [ ] Table of contents if > 1000 words
  - [ ] Examples provided

- [ ] **Formatting**
  - [ ] Code blocks have language specified
  - [ ] Bold/italic used correctly
  - [ ] Lists are consistent
  - [ ] Tables align properly

- [ ] **Links**
  - [ ] All internal links work (relative paths)
  - [ ] External links are current
  - [ ] Anchors link to actual headers
  - [ ] Related docs section complete

- [ ] **Content**
  - [ ] Accurate and current
  - [ ] Examples tested (if code)
  - [ ] No broken references
  - [ ] Metadata updated (date, author)

- [ ] **Navigation**
  - [ ] Registered in README.md
  - [ ] Added to SITEMAP.md
  - [ ] Added to relevant category README
  - [ ] Cross-references updated

---

## 📚 **Document Templates**

### Template: Getting Started Guide
```markdown
# [Service/Feature] Getting Started

**Purpose:** Get [item] running in 5 minutes  
**Audience:** Developers  
**Prerequisites:** [Prerequisites]

## 📋 Requirements

- [Requirement 1]
- [Requirement 2]

## ⚡ Quick Start

1. [Step 1]
2. [Step 2]
3. [Step 3]

## ✅ Verify It Works

- [ ] Check 1
- [ ] Check 2

## 🔗 Next Steps

- [Topic 1](path/file.md)
- [Topic 2](path/file.md)

## ❓ Troubleshooting

**Problem:** [Issue]  
**Solution:** [Fix]
```

### Template: Reference Guide
```markdown
# [Component] Reference

**Purpose:** Complete reference for [component]  
**Audience:** [Who uses this]  
**Last Updated:** [Date]

## 📊 Overview

| Property | Value | Description |
|----------|-------|-------------|
| [Prop 1] | [Val] | [Desc] |

## 🔧 Configuration

### [Section 1]
[Content]

### [Section 2]
[Content]

## 📌 Examples

```bash
# Example
```

## 🔗 See Also

- [Related 1](path/file.md)
- [Related 2](path/file.md)
```

### Template: Troubleshooting Guide
```markdown
# [Component] Troubleshooting

**Purpose:** Fix common issues with [component]  
**Audience:** [Who gets issues]

## ❌ Common Issues

### Issue 1: [Problem Description]

**Symptoms:** [How to recognize]

**Causes:** [Why it happens]

**Solutions:**
1. [Solution 1]
2. [Solution 2]

### Issue 2: [Problem Description]

**Symptoms:** [How to recognize]

**Solutions:**
1. [Solution]

## 🆘 Still Stuck?

- Check [Related Guide](path/file.md)
- See [FAQ](path/file.md)
```

---

## 🎓 **Example: Well-Formatted Document Section**

```markdown
## 🔐 Authentication Setup

### Prerequisites

Before configuring authentication, ensure you have:
- JWT tokens enabled in your service
- Access to Azure Key Vault
- Required NuGet packages installed

### Configuration Steps

1. **Install NuGet Package**
   ```bash
   dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
   ```

2. **Add Configuration**
   ```csharp
   services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddJwtBearer(options =>
       {
           options.Authority = configuration["Auth:Authority"];
       });
   ```

3. **Apply Middleware**
   ```csharp
   app.UseAuthentication();
   app.UseAuthorization();
   ```

### Verification

- [ ] Service starts without errors
- [ ] `GET /health` returns 200
- [ ] `GET /protected` returns 401 (unauthorized)
- [ ] `GET /protected` with valid token returns data

> 💡 **Tip:** Store JWT secrets in Azure Key Vault, not in appsettings.

### Troubleshooting

**Problem:** Getting 401 Unauthorized  
**Solution:** Check that JWT token is current and signed correctly

**Problem:** 403 Forbidden even with valid token  
**Solution:** Verify authorization policies match your token claims

### See Also
- [JWT Best Practices](../security/BEST_PRACTICES.md)
- [Secrets Management](../security/SECRETS_MANAGEMENT.md)
```

---

## 📞 **Questions About Documentation?**

- **Format question?** Check this file
- **Example needed?** See templates above
- **Style question?** See Writing Style Guide
- **Something missing?** Submit an update

---

**Last Updated:** October 27, 2025  
**Maintained By:** Documentation Team  
**Version:** 1.0  
**Status:** ✅ Complete
