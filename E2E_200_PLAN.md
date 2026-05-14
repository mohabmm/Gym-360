# 200 E2E Flow Plan — 2026-05-13

**Goal**: cover 200 critical normal-system flows across API, web, mobile.
**Accounts**: provisioned fresh at cycle start via `scripts/provision_e2e_cycle.py`.
**State**: this file is the matrix. Results appended to `E2E_200_RESULTS.md`. Creds at `tests/e2e_cycle/creds_<ts>.json`.

## Provisioning (setup, not counted)

P1. admin login (`admin@demo.local`/`123456`) → token
P2. POST `/api/v1/gyms` register {autoApprove:true} → gym `e2e_<ts>`
P3. wait for `GymApprovedIntegrationEvent` → poll `/billing/plans/active` until Pro
P4. POST `/api/v1/admin/users` owner `owner_e2e@e2e.local`
P5. POST `/api/v1/admin/gyms/{gymId}/staff/create` Manager `staff_e2e@e2e.local`
P6. POST `/api/v1/identity/users/staff-create-trainer` `trainer_e2e@e2e.local`
P7. POST `/api/v1/gyms/{gymId}/members` trainee `trainee_e2e@e2e.local`
P8. each fresh role logs in + select-gym; dump JWTs to creds JSON.

---

## API — 80 flows (#1–80)

### Identity (1–8)
1. admin login → 200
2. owner login → 200
3. staff login → 200
4. trainer login → 200
5. trainee login → 200
6. refresh-token (trainee) → 200
7. GET /identity/me → 200
8. POST /identity/change-password → 200

### Admin (9–20)
9. GET /admin/users → 200
10. GET /admin/users/{id} → 200
11. PATCH /admin/users/{id} → 200
12. POST /admin/users → 201
13. GET /admin/gyms → 200
14. GET /admin/gyms/pending → 200
15. POST /admin/gyms/{id}/suspend → 200
16. POST /admin/gyms/{id}/reactivate → 200
17. GET /admin/platform-config → 200
18. PATCH /admin/security-settings → 204
19. GET /admin/audit-logs → 200
20. GET /admin/feature-flags → 200

### Gyms (21–32)
21. GET /gyms → 200
22. GET /gyms/{id} → 200
23. PUT /gyms/{id} → 200
24. GET /gyms/{id}/operating-hours → 200
25. PUT /gyms/{id}/operating-hours → 200
26. GET /gyms/{id}/seasonal-hours → 200
27. PUT /gyms/{id}/seasonal-hours → 200
28. GET /gyms/{id}/sections → 200
29. PUT /gyms/{id}/sections → 200
30. GET /gyms/{id}/policies → 200
31. PUT /gyms/{id}/policies → 200
32. GET /gyms/{id}/amenities → 200

### Members + Staff (33–44)
33. GET /gyms/{id}/members → 200
34. GET /gyms/{id}/members/{id} → 200
35. PATCH /gyms/{id}/members/{id} → 200
36. POST /gyms/{id}/members/{id}/freeze → 200
37. POST /gyms/{id}/members/{id}/unfreeze → 200
38. POST /gyms/{id}/members/{id}/notes → 201
39. GET /gyms/{id}/staff → 200
40. POST /gyms/{id}/staff → 201
41. PATCH /gyms/{id}/staff/{id} → 200
42. GET /gyms/{id}/staff/{id} → 200
43. POST /gyms/{id}/staff/{id}/deactivate → 200
44. POST /gyms/{id}/staff/{id}/reactivate → 200

### Trainers (45–50)
45. GET /gyms/{id}/trainers → 200
46. GET /gyms/{id}/trainers/{id} → 200
47. PUT /gyms/{id}/trainers/{id}/rate → 200
48. GET /gyms/{id}/trainers/{id}/availability → 200
49. PUT /gyms/{id}/trainers/{id}/availability → 200
50. POST /scheduling/availability/block-date → 200

### Sessions + Scheduling (51–60)
51. POST /scheduling/sessions (book) → 201
52. GET /scheduling/sessions → 200
53. POST /scheduling/sessions/{id}/confirm → 200
54. POST /scheduling/sessions/{id}/cancel → 200
55. POST /scheduling/sessions/{id}/complete → 200
56. GET /scheduling/sessions/{id} → 200
57. POST /scheduling/sessions/{id}/exercise-logs → 201
58. GET /scheduling/sessions/{id}/exercise-logs → 200
59. GET /scheduling/availability/trainers/{id} → 200
60. GET /scheduling/calendar → 200

### Billing (61–70)
61. GET /billing/plans → 200
62. GET /billing/plans/active → 200
63. POST /billing/plans → 201
64. PUT /billing/plans/{id} → 200
65. GET /billing/package-templates → 200
66. POST /billing/package-templates → 201
67. POST /billing/packages/assign → 201
68. GET /billing/me/packages → 200
69. GET /billing/payments → 200
70. POST /billing/payments/{id}/void → 204

### Training + Progress + Nutrition (71–78)
71. GET /training/exercises → 200
72. POST /training/exercises → 201
73. POST /training/programs → 201
74. POST /training/assignments/program → 201
75. POST /progress/body-metrics → 201
76. GET /progress/body-metrics/trends → 200
77. POST /nutrition/meal-plans → 201
78. POST /nutrition/food-log → 201

### Comms + Gamification (79–80)
79. GET /communication/notifications → 200
80. GET /gamification/trainers/leaderboard → 200

---

## Web — 60 Playwright flows (#81–140)

### Admin web (81–110)
81. Login admin
82. Dashboard KPIs render
83. Gyms list loads
84. Gym detail tabs (overview/members/revenue/settings)
85. Edit gym basics + save
86. Operating hours edit/save
87. Seasonal hours add/save
88. Sections add/save
89. Policy edit/save
90. Suspend gym + reactivate
91. Approve pending gym
92. Reject pending gym
93. Users list + filter
94. User detail open
95. Edit user firstName
96. Create platform user
97. Bulk suspend overdue
98. Plans list
99. Create plan
100. Edit plan
101. Deactivate plan
102. Package templates list
103. Create package template
104. Edit package template
105. Payments list + filter
106. Void payment
107. Audit logs view
108. Feature flags toggle
109. Security settings edit
110. Logout

### Staff web (111–140)
111. Login staff_e2e + select-gym
112. Dashboard renders
113. Members list
114. Member filter (active/frozen)
115. Member detail
116. Add member
117. Edit member
118. Freeze member
119. Unfreeze member
120. Member note add
121. Trainers list
122. Trainer detail
123. Set trainer rate
124. Trainer availability edit
125. Classes list
126. Create class
127. Edit class
128. Schedule class
129. Cancel class
130. Plans list (gym-scoped)
131. Create plan
132. Package templates list
133. Create template
134. Assign package
135. Payments list
136. Record payment
137. Void payment
138. Announcements create
139. Testimonials moderate
140. Logout

---

## Mobile — 60 MobAI + Maestro flows (#141–200)

### Trainee (141–160) — MobAI/Maestro on iOS sim
141. Login trainee_e2e
142. Home renders
143. Workouts tab
144. Sessions tab
145. Book session — pick trainer
146. Book session — pick time
147. Book session — confirm
148. View upcoming sessions
149. Cancel session
150. Session detail
151. Exercise logs view
152. Progress dashboard
153. Body metrics entry
154. Water log
155. Food log entry
156. Meal plan view
157. Notifications list
158. Profile screen
159. PDPL download my-data
160. Logout

### Trainer (161–180)
161. Login trainer_e2e
162. Today schedule
163. Calendar view
164. Client list
165. Client detail
166. Session detail
167. Mark session complete
168. Log exercise actuals
169. Set availability
170. Block date
171. Earnings tab
172. Coach-of-month
173. Programs list
174. Create program
175. Assign program to client
176. Send message to client
177. Notifications
178. Profile + rate display
179. PDPL my-data
180. Logout

### Staff (181–200)
181. Login staff_e2e + select-gym
182. Dashboard tiles
183. Member list
184. Member detail
185. Add member
186. Freeze member
187. Check-in kiosk start
188. Kiosk PIN enter
189. Kiosk check-in by member id
190. Trainers list
191. Set trainer rate
192. Classes list
193. Create class
194. Record payment
195. Void payment
196. Announcement create
197. Testimonials moderate
198. Leaderboard view
199. Settings hub + PDPL section
200. Logout
