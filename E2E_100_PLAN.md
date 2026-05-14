# 100 E2E Test Cases — 2026-05-12

## 40 API (curl/MCP)

### Identity (5)
1. POST /identity/login admin → 200 + token
2. POST /identity/login bad pwd → 401
3. POST /identity/refresh-token valid → 200
4. POST /identity/select-gym → 200 + gym claim
5. GET /identity/me with token → 200

### Admin Users (5)
6. GET /admin/users → 200 + list
7. GET /admin/users/{id} → 200 (just-shipped)
8. PATCH /admin/users/{id} {firstName} → 200 (just-shipped)
9. POST /admin/users new → 201
10. DELETE /admin/users/{id} self → 400 (cannot delete self)

### Gyms (5)
11. GET /gyms → 200
12. GET /gyms/{id} → 200
13. PUT /gyms/{id} {name} → 200 (just-fixed PATCH→PUT)
14. POST /gyms register → 201 (autoApprove)
15. GET /gyms/pending → 200

### Gym Settings (6 — just-shipped)
16. GET /gyms/{id}/operating-hours → 200
17. PUT /gyms/{id}/operating-hours → 200
18. GET /gyms/{id}/sections → 200
19. PUT /gyms/{id}/sections → 200
20. GET /gyms/{id}/seasonal-hours → 200
21. PUT /gyms/{id}/seasonal-hours → 200

### Billing (8)
22. GET /billing/plans → 200
23. POST /billing/plans → 201
24. PUT /billing/plans/{id} → 200 (just-shipped)
25. GET /billing/package-templates → 200
26. POST /billing/package-templates → 201
27. PUT /billing/package-templates/{id} → 200 (just-shipped)
28. GET /billing/payments → 200
29. POST /billing/payments/{id}/void → 200

### Members + Trainers (6)
30. GET /gyms/{id}/members → 200
31. PATCH /gyms/{id}/members/{id} {name,phone} → 200 (just-shipped)
32. POST /gyms/{id}/members/freeze → 200
33. GET /gyms/{id}/trainers → 200
34. GET /gyms/{id}/trainers/{id} → 200
35. PUT /gyms/{id}/trainers/{id}/rate → 200

### Gamification + Misc (5)
36. GET /gamification/testimonials/pending → 200
37. PUT /gamification/testimonials/{id}/moderate {status:approved} → 404 with fake id (just-shipped)
38. GET /gamification/leaderboards/Attendance → 200
39. GET /admin/platform-config → 200
40. PATCH /admin/security-settings → 204

## 40 Web (Playwright admin + staff)

### Admin (20)
41-45. Login + dashboard KPIs render
46-50. Gyms list + filter + detail tabs (overview/members/revenue/settings)
51-55. Settings panel: edit hours/policy/sections/seasonal, save reloads correctly
56-60. Users CRUD + bulk suspend overdue

### Gym Staff (20)
61-65. Login + dashboard
66-70. Members: list, filter, detail, add, freeze, edit
71-75. Classes CRUD + schedule
76-80. Billing: plans create/edit/deactivate, packages create/edit, void payment

## 20 Mobile (MobAI)

### Trainee (7)
81. Login trainee@demo.local
82. Home renders
83. Book session
84. Check session list
85. View profile
86. Download my data (PDPL)
87. Logout

### Trainer (7)
88. Login trainer@demo.local
89. Today schedule
90. Client list
91. Session detail + log exercise
92. Earnings tab
93. PDPL settings
94. Logout

### GymStaff (6)
95. Login staff@demo.local
96. Member list
97. Check-in kiosk
98. Settings hub renders PDPL section (just-shipped)
99. Download my data tap
100. Logout
