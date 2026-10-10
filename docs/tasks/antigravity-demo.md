# Antigravity: demonstration interface and keyboard behavior

Continue the Bancada task in D:/Eu/Projetos/1/.worktrees/antigravity only, branch agent/antigravity-frontend. Codex integrated your frontend and independently verified build and16tests. Codex fixed CSRF login caching, staff guard, quote precision, contrast and mobile overflow in root commit b202b26. That commit is now cherry-picked into your worktree; preserve these fixes. The complete three-role browser repair journey passed.

Own frontend plus docs/tasks/antigravity-demo-result.md only. No backend/scripts/commit/push/deploy.

Implement demo UI for task8 contract being implemented by Copilot: GET/api/demo->{enabled}; POST/api/demo/start{} -> existingSessionInfo; POST/api/demo/role{role:receptionist|technician1|technician2}->SessionInfo; POST/api/demo/reset{}->SessionInfo. Each mutationCSRFprotected, invalidate cachedCSRF afteridentitychanges. Mode disabled uses regularlogin. Enabledlogin presents honest private demonstration entry button, explains fictional data20orders24h; no sharedpassword/accounts. Application shows restraineddemo strip with currentrole and selectable3roles and explicit resetconfirmation, no fake metrics. Handleexpired401 andquotafailure with readable feedback. Reset navigates toqueue afternewsession; role switch resets sessiondata viaAPI and reloads relevantroute not browserreload workaround.

Test-first newdemo UI/APIcases. Existing19tests mustpass and build. Don't mock realproductAPI; tests maymock isolatedHttpClient as normalunit tests. Ensure destructive fictional reset requires explicit productconfirmation. This is disposable private demo data approved byuser, no outsidefilesdata.

Fix keyboard behavior in customerapproval andcancel dialogs: focus entersdialog, Tab/ShiftTab stays inside, Esc closes unlessmutationpending, restorefocus totrigger; no hiddenfocus, accessible title. Existing productapprovalconfirmation remains. Add meaningfulkeyboardtests.

Remove speculativequery-token fallback: access token officiallyfragmentonly, never queryURLs. Publicmessages mustclaim recordeddecision, no emailnotification because noneexists. Keep decimalstrings: do not silently round quantity/prices. ReportactualRED/GREENcommands/counts, scope andlimitations. Contractendpointnamesmustmatchabove. Returnautonomously.

