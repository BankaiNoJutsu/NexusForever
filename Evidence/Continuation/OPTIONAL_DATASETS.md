# Optional inputs outside the lean handoff

The repository, its assessment/PR triage, durable RE notes and lean evidence store
are enough to continue source implementation and run the normal build/tests.
The owner explicitly chose to omit the roughly 491 MiB full evidence draft.
The following inputs are conditional on the later work being attempted.

| Input | Needed for | Source-machine location or recovery route |
| --- | --- | --- |
| WildStar build16042 installation, archives and prepared table/language binaries | Fresh native observation, table extraction and native-client qualification | Operator-supplied installation; the working source client was `I:\Wildstar`. Use the repo extraction/setup guides. Client binaries and Ghidra project databases are excluded. |
| General decompiler cache (50,302 files) and analysis logs | Broad address searches and export regeneration beyond the focused traces | `I:\GIT\NexusForever-assessment\Decomp\Analysis\exports` and `logs`. Regenerate from the supplied exact-build client using the repo Ghidra scripts. Focused evidence and durable labels are in Git. |
| Recovered Jabbithole static SQL (17 files) | DataMapping authoring and spawn/reference regeneration | `I:\GIT\NexusForever-assessment\jabbithole_mysql`. Copy these non-sensitive reference dumps separately if needed, or recover an appropriate public mirror. Their complete historical upstream dataset is not available here. Some reference data cannot be recreated from the client alone. |
| Recovered client-table SQL (79 files) | DataMapping authoring/table queries | `I:\GIT\NexusForever-assessment\wildstar_client_mysql`. Copy separately or regenerate the required tables from an operator-supplied client. |
| Generated mapping CSVs (59 output files, two review files) | Reviewing/regenerating authoring candidates | `I:\GIT\NexusForever-assessment\Tools\DataMapping\output` and `review`. Regenerate from the reference sources with the DataMapping scripts. Checked-in safe runtime seeds remain available for normal deployment. |
| Full historical report and old checkpoints | Auditing superseded proofs or exact historical replay | `I:\GIT\NexusForever-assessment-report`. Latest harnesses and selected authoritative checkpoint proof are in Git. Older frozen binaries must be rebuilt or recovered separately; old absolute paths/PIDs are not destination state. |
| Three tutorial videos | Another visual tutorial review | `I:\Videos`; recorded hashes/provenance are preserved in the evidence. Media is excluded. |
| June19 Q5597 UI smoke bundle | Original historical UI evidence | Unavailable, as confirmed by the owner. Do not request it again or relabel replacement material as the original UI proof. |

Fresh MySQL/RabbitMQ instances and fresh credentials/configuration are needed for
provider or live-stack checks. Private databases/accounts, credentials, live
service/client logs, broker state and old agent authentication are excluded and
must not be copied into Git. Recreate isolated synthetic fixtures; do not assume
the new machine inherits the old runtime or private account state.

Install the SDK/Python and, when required, Node/REA/Ghidra/JDK/debugger tools listed
in the main handoff. The archived skill folders contain instructions, not tool
binaries or agent authentication. Review machine-specific setup before applying
configuration. Computer Use/native launch remain stopped until explicitly resumed.
