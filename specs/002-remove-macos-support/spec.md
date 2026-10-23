# Feature Specification: Remove macOS Support

**Feature Branch**: `002-remove-macos-support`

**Created**: 2026-10-22

**Status**: Draft

**Input**: User description: "Remove MACOS support."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The toolkit no longer promises macOS (Priority: P1)

A person reading the README, the user guide or the product overview to decide whether DMC runs on their computer
sees Windows and Linux listed as supported, and macOS (Apple Silicon) is not mentioned as a supported system anywhere.
The same holds for every document a developer reads to build, test or release the toolkit.

**Why this priority**: The supported-systems list is the promise the toolkit makes. If it still names macOS while the
build no longer produces it, a Mac user installs something that cannot work. This is the part of the removal a user
sees first, and it can ship on its own.

**Independent Test**: Search every user-facing and developer-facing document for macOS, Mac, Apple Silicon, Darwin and
the `osx` platform name. Each match is either gone or sits in a place that deliberately records history (a past
decision or a past release), and says so.

**Acceptance Scenarios**:

1. **Given** the README and the user guide, **When** a reader looks for the systems DMC runs on, **Then** they find
   Windows (x64 and Arm) and Linux (x64 and Arm) and no mention of macOS as a supported system.
2. **Given** the instructions for starting a session, **When** a reader looks for which launcher to use, **Then** the
   launcher for macOS, Linux or Git Bash is described for Linux and Git Bash only.
3. **Given** the developer, release and architecture documents, **When** a reader looks for the platforms the tests
   and the releases cover, **Then** macOS is not listed among them.
4. **Given** the decision log, **When** a reader looks at the entries about platforms, **Then** the decision to ship
   for macOS is marked as superseded by this one, with the reason, and the new decision is recorded.

---

### User Story 2 - Releases and automated checks stop building for macOS (Priority: P1)

The maintainer publishes a release and gets archives for Windows and Linux only. The automated checks that run on every
change no longer spend time on a macOS machine, and no release step waits for, or fails on, a macOS build.

**Why this priority**: This is what actually stops macOS support. Until the release and the checks stop producing and
testing the macOS build, the toolkit would still ship something it no longer promises or verifies.

**Independent Test**: Run the release process and the per-change checks on a branch with this change. The release
lists archives for Windows and Linux only, the recorded checksums name no macOS archive, and the checks pass without a
macOS run.

**Acceptance Scenarios**:

1. **Given** a release is cut, **When** it finishes, **Then** its archives are for Windows x64, Windows Arm,
   Linux x64 and Linux Arm, and none is for macOS.
2. **Given** the release's recorded list of archives and their checksums, **When** it is published, **Then** it names
   only the four remaining platforms.
3. **Given** a change is pushed, **When** the automated checks run, **Then** they cover Windows and Linux and start no
   macOS job.
4. **Given** earlier releases that already contain a macOS archive, **When** this change is released, **Then** those
   earlier releases are left as they are.

---

### User Story 3 - A Mac user gets a clear answer, not a broken install (Priority: P2)

Someone on a Mac who runs the setup script that fetches the toolkit's helper program is told plainly that macOS is not
supported, instead of being offered a download that was never built or a confusing failure.

**Why this priority**: People who used macOS before will still try. A clear refusal costs little and saves them a
puzzling failure, but it matters only after the build and the documents no longer include macOS.

**Independent Test**: Run each setup script, in a test that pretends to be a Mac, and check that it stops with a
message naming the supported systems and downloads nothing.

**Acceptance Scenarios**:

1. **Given** the shell setup script on a Mac, **When** it runs, **Then** it stops with a message that macOS is not
   supported and that names Windows and Linux, and it downloads nothing.
2. **Given** the PowerShell setup script on a Mac, **When** it runs, **Then** it stops with the same message.
3. **Given** the setup scripts on Windows and on Linux, **When** they run, **Then** they behave exactly as before.

---

### User Story 4 - macOS-only code and test cases are removed (Priority: P3)

A developer reading the helper program's source and its tests finds no branch, platform declaration, comment or test
case that exists only for macOS, so nothing in the code describes behavior the toolkit no longer has.

**Why this priority**: This is housekeeping. Leaving the code would not break anyone, but dead branches and stale
comments mislead the next reader and keep untestable paths alive.

**Independent Test**: Search the source and the tests for macOS, Darwin and Apple. No code path or test case remains
whose only purpose is macOS, and the full test suite passes on Windows and Linux.

**Acceptance Scenarios**:

1. **Given** the helper program opens a map in the default browser, **When** it runs on Windows or Linux, **Then** it
   opens the same program as before, and the code has no macOS opener.
2. **Given** the test suite, **When** it runs on Windows and Linux, **Then** every test passes and none checks macOS
   behavior.
3. **Given** the comments that describe platform differences, **When** they are read, **Then** they name only
   Windows and Linux.

---

### Edge Cases

- A Mac user who already installed a helper program from an earlier release keeps a working copy, since nothing
  is deleted from their machine. The toolkit no longer fixes problems on it, and a newer release gives them no macOS
  archive to update to.
- A Linux or Windows user whose setup script reports a system name the script does not recognize still gets the
  existing "not built for this system" message, which now lists Windows and Linux.
- A Linux shell running under Windows (Git Bash) is not a Mac and keeps working.
- Past entries in the decision log and the roadmap that mention macOS stay as history; they are marked as superseded
  where a reader would otherwise take them as current.
- A release made before this change keeps its macOS archive and checksum; only releases made after it stop including one.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The documents that tell users which systems DMC runs on MUST list Windows (x64 and Arm) and Linux (x64
  and Arm) and MUST NOT name macOS as supported.
- **FR-002**: The instructions for starting a session MUST describe the shell launcher for Linux and Git Bash only.
- **FR-003**: The release process MUST NOT build, package, checksum or publish a macOS archive.
- **FR-004**: The recorded list of archives and checksums that the setup scripts rely on MUST NOT contain a macOS
  entry, and the process that writes it MUST NOT expect one.
- **FR-005**: The automated checks run on each change MUST NOT include a macOS run.
- **FR-006**: Both setup scripts MUST refuse to run on macOS with a message that says macOS is not supported and
  names the supported systems, MUST download nothing, and MUST end with a failure result.
- **FR-007**: The setup scripts MUST behave exactly as before on Windows and Linux.
- **FR-008**: The helper program's source MUST contain no code path, platform declaration or comment that exists only
  for macOS.
- **FR-009**: The test suite MUST contain no test case that runs macOS code or expects macOS behavior, MUST cover the
  refusal described in FR-006 (the one test allowed to name macOS), and MUST pass on Windows and Linux.
- **FR-010**: The developer, release and architecture documents MUST describe the platform matrix, the build
  prerequisites and the runtime identifiers without macOS.
- **FR-011**: The decision to ship for macOS MUST be marked superseded, and a new decision recording the removal
  and its reason MUST be added to the decision log. The roadmap MUST record this change.
- **FR-012**: Releases made before this change MUST NOT be altered or withdrawn.

### Key Entities

- **Supported system**: A combination of operating system and processor the toolkit is built, tested and documented
  for. After this change: Windows x64, Windows Arm, Linux x64, Linux Arm.
- **Release archive**: The downloadable package of the helper program for one supported system, with its checksum.
- **Release manifest**: The recorded list of archives and checksums the setup scripts check a download against.
- **Superseded decision**: A decision-log entry kept as history and marked as replaced by a later one.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A search of all user-facing and developer-facing documents for macOS, Mac, Apple, Darwin and the `osx`
  platform name returns no statement that macOS is supported; every remaining match is a marked piece of history, a statement that
  macOS is not supported, or a factual note about operating-system behavior.
- **SC-002**: The next release contains exactly four archives (Windows x64, Windows Arm, Linux x64, Linux Arm) and
  zero macOS archives.
- **SC-003**: The automated checks for a change run on exactly two operating systems (Windows and Linux) and all pass.
- **SC-004**: A Mac user who runs either setup script gets a refusal that names the supported systems within 5 seconds,
  with no download started.
- **SC-005**: The full test suite passes on Windows and Linux with no macOS-only test case remaining.
- **SC-006**: A search of the source and the tests for macOS, Darwin and Apple returns no code path or test case that
  runs macOS code; any remaining match is the macOS refusal in the setup scripts, its test, or a comment that says why
  it is refused.

## Assumptions

- "Remove macOS support" means the toolkit stops building, testing, documenting and promising macOS. It does not mean
  actively blocking or breaking a copy someone already has on a Mac.
- The setup scripts keep a small macOS check, solely to give a clear refusal. Removing the check as well would turn a
  clear message into a confusing "no archive for this system" failure.
- Earlier releases, their macOS archives and the history in the decision log stay in place. Only the decision log's
  current-state markings and the roadmap change.
- Intel Macs were already unsupported; this change does not alter that, and the removal covers Apple Silicon.
- Git Bash and Linux shells keep the shell launcher; only the macOS wording goes.
- No user data is affected: campaigns, modules and the rules reference are plain files and play the same on
  Windows and Linux.
- The removal is announced in the release notes of the first release that has no macOS archive.
