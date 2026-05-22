import type { MemberListItem, TrainerListItem, GymClass, PaymentListItem, Announcement, LeaderboardEntry, TrainerRankingEntry, Testimonial, MemberCheckIn, StaffMember } from '@gym360/ui';

// ──── First Gym Staff ────
export const gymStaff: StaffMember[] = [
  { id: 's1', firstName: 'Mostafa', lastName: 'Kamel', firstNameAr: 'مصطفى', lastNameAr: 'كامل', email: 'mostafa@firstgym.com', phone: '+201001112233', role: 'gym_owner', status: 'active', staffSubRole: 'owner', gymId: 'g1', hireDate: '2023-06-15', permissions: ['manage_members', 'manage_trainers', 'manage_billing', 'manage_classes', 'manage_staff', 'manage_settings', 'view_analytics', 'manage_announcements', 'manage_check_ins'], createdAt: '2023-06-15' },
  { id: 's2', firstName: 'Youssef', lastName: 'Nabil', firstNameAr: 'يوسف', lastNameAr: 'نبيل', email: 'youssef@firstgym.com', phone: '+201002223344', role: 'gym_staff', status: 'active', staffSubRole: 'manager', gymId: 'g1', hireDate: '2023-08-01', permissions: ['manage_members', 'manage_trainers', 'manage_classes', 'manage_staff', 'manage_announcements', 'manage_check_ins', 'view_analytics'], createdAt: '2023-08-01' },
  { id: 's3', firstName: 'Salma', lastName: 'Adel', firstNameAr: 'سلمى', lastNameAr: 'عادل', email: 'salma@firstgym.com', phone: '+201003334455', role: 'gym_staff', status: 'active', staffSubRole: 'receptionist', gymId: 'g1', hireDate: '2024-01-15', permissions: ['manage_check_ins', 'manage_members'], createdAt: '2024-01-15' },
  { id: 's4', firstName: 'Noha', lastName: 'Ahmed', firstNameAr: 'نهى', lastNameAr: 'أحمد', email: 'noha@firstgym.com', phone: '+201004445566', role: 'gym_staff', status: 'active', staffSubRole: 'receptionist', gymId: 'g1', hireDate: '2024-03-01', permissions: ['manage_check_ins'], createdAt: '2024-03-01' },
];

// ──── Members (First Gym) ────
export const members: MemberListItem[] = [
  { id: 'm1', name: 'Ahmed El-Sayed', nameAr: 'أحمد السيد', email: 'ahmed.s@email.com', phone: '+201111222333', status: 'active', membershipType: 'annual', joinDate: '2024-01-15', expiryDate: '2025-01-15', section: 'Mens', lastCheckIn: '2026-04-12', avatarUrl: undefined },
  { id: 'm2', name: 'Mariam Khalil', nameAr: 'مريم خليل', email: 'mariam.k@email.com', phone: '+201112223334', status: 'active', membershipType: 'monthly', joinDate: '2025-11-01', expiryDate: '2026-05-01', section: 'Womens', lastCheckIn: '2026-04-11' },
  { id: 'm3', name: 'Omar Tawfik', nameAr: 'عمر توفيق', email: 'omar.t@email.com', phone: '+201113224335', status: 'active', membershipType: 'quarterly', joinDate: '2025-09-10', expiryDate: '2026-06-10', section: 'Mens', lastCheckIn: '2026-04-12' },
  { id: 'm4', name: 'Fatma Abdel-Rahman', nameAr: 'فاطمة عبد الرحمن', email: 'fatma.ar@email.com', phone: '+201114225336', status: 'active', membershipType: 'annual', joinDate: '2024-06-01', expiryDate: '2025-06-01', section: 'Womens', lastCheckIn: '2026-04-10' },
  { id: 'm5', name: 'Hassan Medhat', nameAr: 'حسن مدحت', email: 'hassan.m@email.com', phone: '+201115226337', status: 'active', membershipType: 'session_package', joinDate: '2025-12-01', expiryDate: '2026-06-01', section: 'Mens', lastCheckIn: '2026-04-09' },
  { id: 'm6', name: 'Nada El-Masry', nameAr: 'ندى المصري', email: 'nada.m@email.com', phone: '+201116227338', status: 'frozen', membershipType: 'monthly', joinDate: '2025-10-15', expiryDate: '2026-04-15', section: 'Womens', lastCheckIn: '2026-03-01' },
  { id: 'm7', name: 'Yasser Abdallah', nameAr: 'ياسر عبدالله', email: 'yasser.a@email.com', phone: '+201117228339', status: 'active', membershipType: 'semi_annual', joinDate: '2025-08-01', expiryDate: '2026-08-01', section: 'Mens', lastCheckIn: '2026-04-12' },
  { id: 'm8', name: 'Laila Samir', nameAr: 'ليلى سمير', email: 'laila.s@email.com', phone: '+201118229340', status: 'active', membershipType: 'annual', joinDate: '2024-03-01', expiryDate: '2025-03-01', section: 'Womens', lastCheckIn: '2026-04-11' },
  { id: 'm9', name: 'Kareem Mostafa', nameAr: 'كريم مصطفى', email: 'kareem.m@email.com', phone: '+201119230341', status: 'expired', membershipType: 'monthly', joinDate: '2026-02-01', expiryDate: '2026-03-01', section: 'Mens', lastCheckIn: '2026-02-28' },
  { id: 'm10', name: 'Rana Gamal', nameAr: 'رنا جمال', email: 'rana.g@email.com', phone: '+201120231342', status: 'active', membershipType: 'quarterly', joinDate: '2025-12-15', expiryDate: '2026-06-15', section: 'Womens', lastCheckIn: '2026-04-12' },
  { id: 'm11', name: 'Ayman Hossam', nameAr: 'أيمن حسام', email: 'ayman.h@email.com', phone: '+201121232343', status: 'active', membershipType: 'annual', joinDate: '2024-04-01', expiryDate: '2025-04-01', section: 'Mens', lastCheckIn: '2026-04-11' },
  { id: 'm12', name: 'Dina Fawzy', nameAr: 'دينا فوزي', email: 'dina.f@email.com', phone: '+201122233344', status: 'active', membershipType: 'monthly', joinDate: '2026-03-01', expiryDate: '2026-05-01', section: 'Womens', lastCheckIn: '2026-04-12' },
  { id: 'm13', name: 'Mahmoud Sherif', nameAr: 'محمود شريف', email: 'mahmoud.s@email.com', phone: '+201123234345', status: 'active', membershipType: 'session_package', joinDate: '2025-11-15', expiryDate: '2026-05-15', section: 'Mens', lastCheckIn: '2026-04-10' },
  { id: 'm14', name: 'Reem Abdel-Hamid', nameAr: 'ريم عبد الحميد', email: 'reem.ah@email.com', phone: '+201124235346', status: 'active', membershipType: 'semi_annual', joinDate: '2025-07-01', expiryDate: '2026-07-01', section: 'Womens', lastCheckIn: '2026-04-09' },
  { id: 'm15', name: 'Tamer Ragab', nameAr: 'تامر رجب', email: 'tamer.r@email.com', phone: '+201125236347', status: 'inactive', membershipType: 'monthly', joinDate: '2025-09-01', expiryDate: '2025-10-01', section: 'Mens' },
  { id: 'm16', name: 'Yasmin Khaled', nameAr: 'ياسمين خالد', email: 'yasmin.k@email.com', phone: '+201126237348', status: 'active', membershipType: 'quarterly', joinDate: '2026-01-10', expiryDate: '2026-07-10', section: 'Womens', lastCheckIn: '2026-04-12' },
  { id: 'm17', name: 'Hossam Abdel-Fattah', nameAr: 'حسام عبد الفتاح', email: 'hossam.af@email.com', phone: '+201127238349', status: 'active', membershipType: 'annual', joinDate: '2024-02-15', expiryDate: '2025-02-15', section: 'Mens', lastCheckIn: '2026-04-12' },
  { id: 'm18', name: 'Aisha Mohamed', nameAr: 'عائشة محمد', email: 'aisha.m@email.com', phone: '+201128239350', status: 'active', membershipType: 'monthly', joinDate: '2026-03-15', expiryDate: '2026-05-15', section: 'Womens', lastCheckIn: '2026-04-11' },
  { id: 'm19', name: 'Fady Samir', nameAr: 'فادي سمير', email: 'fady.s@email.com', phone: '+201129240351', status: 'active', membershipType: 'semi_annual', joinDate: '2025-10-01', expiryDate: '2026-10-01', section: 'Mens', lastCheckIn: '2026-04-10' },
  { id: 'm20', name: 'Noura Adel', nameAr: 'نورا عادل', email: 'noura.a@email.com', phone: '+201130241352', status: 'active', membershipType: 'annual', joinDate: '2024-05-01', expiryDate: '2025-05-01', section: 'Womens', lastCheckIn: '2026-04-12' },
];

// ──── Trainers (First Gym) ────
export const trainers: TrainerListItem[] = [
  { id: 't1', name: 'Captain Mohamed Ali', nameAr: 'كابتن محمد علي', status: 'active', specializations: ['Strength', 'Bodybuilding'], activeMemberCount: 18, rating: 4.8, monthlyScore: 92 },
  { id: 't2', name: 'Captain Laila Ibrahim', nameAr: 'كابتن ليلى إبراهيم', status: 'active', specializations: ['Yoga', 'Pilates', 'Flexibility'], activeMemberCount: 22, rating: 4.9, monthlyScore: 95 },
  { id: 't3', name: 'Captain Hassan Medhat', nameAr: 'كابتن حسن مدحت', status: 'active', specializations: ['CrossFit', 'HIIT'], activeMemberCount: 15, rating: 4.7, monthlyScore: 88 },
  { id: 't4', name: 'Captain Nour El-Din', nameAr: 'كابتن نور الدين', status: 'active', specializations: ['Cardio', 'Weight Loss'], activeMemberCount: 20, rating: 4.6, monthlyScore: 85 },
  { id: 't5', name: 'Captain Rania Sherif', nameAr: 'كابتن رانيا شريف', status: 'active', specializations: ['Ladies Fitness', 'Nutrition'], activeMemberCount: 16, rating: 4.9, monthlyScore: 94 },
  { id: 't6', name: 'Captain Amr Soliman', nameAr: 'كابتن عمرو سليمان', status: 'pending_approval', specializations: ['Boxing', 'MMA'], activeMemberCount: 0, rating: 0, monthlyScore: 0 },
];

// ──── Classes (First Gym) ────
export const classes: GymClass[] = [
  { id: 'c1', name: 'Morning Yoga', nameAr: 'يوجا صباحية', trainerId: 't2', trainerName: 'Captain Laila', gymId: 'g1', section: 'Womens', capacity: 20, enrolledCount: 18, dayOfWeek: 0, startTime: '07:00', endTime: '08:00', isRecurring: true, status: 'active', color: '#8B5CF6' },
  { id: 'c2', name: 'Power Lifting', nameAr: 'رفع أثقال', trainerId: 't1', trainerName: 'Captain Mohamed', gymId: 'g1', section: 'Mens', capacity: 15, enrolledCount: 12, dayOfWeek: 1, startTime: '18:00', endTime: '19:30', isRecurring: true, status: 'active', color: '#EF4444' },
  { id: 'c3', name: 'HIIT Circuit', nameAr: 'دائرة HIIT', trainerId: 't3', trainerName: 'Captain Hassan', gymId: 'g1', section: 'Mixed', capacity: 25, enrolledCount: 22, dayOfWeek: 2, startTime: '19:00', endTime: '20:00', isRecurring: true, status: 'active', color: '#F59E0B' },
  { id: 'c4', name: 'Pilates', nameAr: 'بيلاتيس', trainerId: 't2', trainerName: 'Captain Laila', gymId: 'g1', section: 'Womens', capacity: 15, enrolledCount: 15, dayOfWeek: 3, startTime: '10:00', endTime: '11:00', isRecurring: true, status: 'full', color: '#10B981' },
  { id: 'c5', name: 'Cardio Blast', nameAr: 'كارديو مكثف', trainerId: 't4', trainerName: 'Captain Nour', gymId: 'g1', section: 'Mixed', capacity: 30, enrolledCount: 24, dayOfWeek: 4, startTime: '17:00', endTime: '18:00', isRecurring: true, status: 'active', color: '#3B82F6' },
  { id: 'c6', name: 'Ladies Bootcamp', nameAr: 'بوت كامب سيدات', trainerId: 't5', trainerName: 'Captain Rania', gymId: 'g1', section: 'Womens', capacity: 20, enrolledCount: 16, dayOfWeek: 5, startTime: '09:00', endTime: '10:00', isRecurring: true, status: 'active', color: '#EC4899' },
];

// ──── Payments ────
export const recentPayments: PaymentListItem[] = [
  { id: 'pay1', memberName: 'Ahmed El-Sayed', amount: 5000, status: 'completed', method: 'cash', type: 'membership', createdAt: '2026-04-12T10:30:00' },
  { id: 'pay2', memberName: 'Mariam Khalil', amount: 800, status: 'completed', method: 'vodafone_cash', type: 'membership', createdAt: '2026-04-11T14:20:00' },
  { id: 'pay3', memberName: 'Hassan Medhat', amount: 3000, status: 'completed', method: 'card', type: 'session_package', createdAt: '2026-04-10T09:15:00' },
  { id: 'pay4', memberName: 'Nada El-Masry', amount: 800, status: 'voided', method: 'cash', type: 'membership', createdAt: '2026-04-09T16:45:00' },
  { id: 'pay5', memberName: 'Yasser Abdallah', amount: 2500, status: 'completed', method: 'instapay', type: 'membership', createdAt: '2026-04-08T11:00:00' },
];

// ──── Announcements ────
export const announcements: Announcement[] = [
  { id: 'a1', gymId: 'g1', title: 'Ramadan Hours', titleAr: 'مواعيد رمضان', body: 'Our operating hours will change during Ramadan. New hours: 6 AM - 12 AM.', bodyAr: 'ستتغير مواعيد العمل خلال شهر رمضان. المواعيد الجديدة: 6 صباحا - 12 منتصف الليل.', targetAudience: 'all', isActive: true, publishedAt: '2026-04-01T08:00:00', createdBy: 's1', createdByName: 'Mostafa Kamel' },
  { id: 'a2', gymId: 'g1', title: 'New HIIT Classes', titleAr: 'حصص HIIT جديدة', body: 'We are excited to announce new HIIT classes every Tuesday and Thursday!', bodyAr: 'يسعدنا الإعلان عن حصص HIIT جديدة كل ثلاثاء وخميس!', targetAudience: 'members', isActive: true, publishedAt: '2026-03-25T10:00:00', createdBy: 's2', createdByName: 'Youssef Nabil' },
];

// ──── Leaderboard ────
export const leaderboard: LeaderboardEntry[] = [
  { rank: 1, memberId: 'm7', memberName: 'Yasser Abdallah', memberNameAr: 'ياسر عبدالله', score: 2450, workoutsCompleted: 28, streakDays: 45, change: 0 },
  { rank: 2, memberId: 'm1', memberName: 'Ahmed El-Sayed', memberNameAr: 'أحمد السيد', score: 2280, workoutsCompleted: 26, streakDays: 32, change: 1 },
  { rank: 3, memberId: 'm17', memberName: 'Hossam Abdel-Fattah', memberNameAr: 'حسام عبد الفتاح', score: 2150, workoutsCompleted: 24, streakDays: 28, change: -1 },
  { rank: 4, memberId: 'm3', memberName: 'Omar Tawfik', memberNameAr: 'عمر توفيق', score: 1980, workoutsCompleted: 22, streakDays: 21, change: 2 },
  { rank: 5, memberId: 'm11', memberName: 'Ayman Hossam', memberNameAr: 'أيمن حسام', score: 1850, workoutsCompleted: 20, streakDays: 18, change: 0 },
];

// ──── Trainer Rankings ────
export const trainerRankings: TrainerRankingEntry[] = [
  { rank: 1, trainerId: 't2', trainerName: 'Captain Laila Ibrahim', trainerNameAr: 'كابتن ليلى إبراهيم', monthlyScore: 95, rating: 4.9, totalRatings: 48, sessionsCompleted: 64, memberSatisfaction: 97 },
  { rank: 2, trainerId: 't5', trainerName: 'Captain Rania Sherif', trainerNameAr: 'كابتن رانيا شريف', monthlyScore: 94, rating: 4.9, totalRatings: 36, sessionsCompleted: 58, memberSatisfaction: 96 },
  { rank: 3, trainerId: 't1', trainerName: 'Captain Mohamed Ali', trainerNameAr: 'كابتن محمد علي', monthlyScore: 92, rating: 4.8, totalRatings: 52, sessionsCompleted: 72, memberSatisfaction: 94 },
];

// ──── Testimonials ────
export const testimonials: Testimonial[] = [
  { id: 'test1', memberId: 'm1', memberName: 'Ahmed El-Sayed', trainerId: 't1', trainerName: 'Captain Mohamed Ali', content: 'Best trainer I have ever worked with. Completely transformed my physique in 6 months.', rating: 5, status: 'approved', createdAt: '2026-04-10' },
  { id: 'test2', memberId: 'm10', memberName: 'Rana Gamal', trainerId: 't2', trainerName: 'Captain Laila Ibrahim', content: 'Her yoga sessions are amazing. Very professional and caring.', rating: 5, status: 'pending', createdAt: '2026-04-11' },
  { id: 'test3', memberId: 'm3', memberName: 'Omar Tawfik', trainerId: 't3', trainerName: 'Captain Hassan Medhat', content: 'Great CrossFit coach, pushes you to your limits!', rating: 4, status: 'approved', createdAt: '2026-04-08' },
];

// ──── Today's Check-Ins ────
export const todayCheckIns: MemberCheckIn[] = [
  { id: 'ci1', memberId: 'm1', memberName: 'Ahmed El-Sayed', checkInTime: '2026-04-12T06:30:00', method: 'qr', section: 'Mens' },
  { id: 'ci2', memberId: 'm3', memberName: 'Omar Tawfik', checkInTime: '2026-04-12T07:15:00', method: 'qr', section: 'Mens' },
  { id: 'ci3', memberId: 'm10', memberName: 'Rana Gamal', checkInTime: '2026-04-12T07:45:00', method: 'numpad', section: 'Womens' },
  { id: 'ci4', memberId: 'm7', memberName: 'Yasser Abdallah', checkInTime: '2026-04-12T08:00:00', method: 'qr', section: 'Mens' },
  { id: 'ci5', memberId: 'm12', memberName: 'Dina Fawzy', checkInTime: '2026-04-12T08:30:00', method: 'manual', section: 'Womens' },
  { id: 'ci6', memberId: 'm16', memberName: 'Yasmin Khaled', checkInTime: '2026-04-12T09:00:00', method: 'qr', section: 'Womens' },
  { id: 'ci7', memberId: 'm17', memberName: 'Hossam Abdel-Fattah', checkInTime: '2026-04-12T09:15:00', method: 'qr', section: 'Mens' },
  { id: 'ci8', memberId: 'm20', memberName: 'Noura Adel', checkInTime: '2026-04-12T09:45:00', method: 'numpad', section: 'Womens' },
];

// ──── Revenue data ────
export const monthlyRevenue = [
  { month: 'Nov', revenue: 38000 },
  { month: 'Dec', revenue: 41000 },
  { month: 'Jan', revenue: 42500 },
  { month: 'Feb', revenue: 40000 },
  { month: 'Mar', revenue: 44000 },
  { month: 'Apr', revenue: 45000 },
];

export const peakHours = [
  { hour: '6AM', mon: 15, tue: 12, wed: 14, thu: 16, fri: 8, sat: 25, sun: 20 },
  { hour: '7AM', mon: 25, tue: 22, wed: 28, thu: 24, fri: 12, sat: 35, sun: 30 },
  { hour: '8AM', mon: 20, tue: 18, wed: 22, thu: 20, fri: 10, sat: 30, sun: 28 },
  { hour: '9AM', mon: 12, tue: 10, wed: 14, thu: 12, fri: 8, sat: 22, sun: 20 },
  { hour: '5PM', mon: 35, tue: 38, wed: 32, thu: 36, fri: 20, sat: 15, sun: 18 },
  { hour: '6PM', mon: 45, tue: 48, wed: 42, thu: 46, fri: 25, sat: 20, sun: 22 },
  { hour: '7PM', mon: 40, tue: 42, wed: 38, thu: 40, fri: 22, sat: 18, sun: 20 },
  { hour: '8PM', mon: 30, tue: 32, wed: 28, thu: 30, fri: 18, sat: 12, sun: 15 },
  { hour: '9PM', mon: 18, tue: 20, wed: 16, thu: 18, fri: 10, sat: 8, sun: 10 },
];
