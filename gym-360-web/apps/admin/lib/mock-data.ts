import type { GymListItem, PendingGym, AdminUser, SubscriptionPlan, Feature, PlanFeature, FeatureOverride, PlanChangeLog, AtRiskGym, PlanDistribution, TimeSeriesPoint, CityRevenue, FeatureUsage } from '@gymapp/ui';

// ──── Admin Users ────
export const adminUsers: (AdminUser & { isYou?: boolean })[] = [
  { id: '1', email: 'ahmed.hassan@gymapp.com', phone: '+201001234567', firstName: 'Ahmed', lastName: 'Hassan', firstNameAr: 'أحمد', lastNameAr: 'حسن', role: 'platform_admin', adminRole: 'super_admin', status: 'active', createdAt: '2024-01-01', isYou: true },
  { id: '2', email: 'sara.mostafa@gymapp.com', phone: '+201002345678', firstName: 'Sara', lastName: 'Mostafa', firstNameAr: 'سارة', lastNameAr: 'مصطفى', role: 'platform_admin', adminRole: 'admin', status: 'active', createdAt: '2024-02-15' },
  { id: '3', email: 'omar.farid@gymapp.com', phone: '+201003456789', firstName: 'Omar', lastName: 'Farid', firstNameAr: 'عمر', lastNameAr: 'فريد', role: 'platform_admin', adminRole: 'support', status: 'active', createdAt: '2024-03-10' },
  { id: '4', email: 'nour.ibrahim@gymapp.com', phone: '+201004567890', firstName: 'Nour', lastName: 'Ibrahim', firstNameAr: 'نور', lastNameAr: 'إبراهيم', role: 'platform_admin', adminRole: 'analyst', status: 'active', createdAt: '2024-04-01' },
];

// ──── Gyms ────
export const gyms: GymListItem[] = [
  { id: 'g1', name: 'First Gym', nameAr: 'فيرست جيم', gymCode: 'FG001', status: 'active', city: 'Cairo', planName: 'Pro', activeMemberCount: 450, activeTrainerCount: 12, paymentStatus: 'current', createdAt: '2023-06-15', ownerName: 'Mostafa Kamel', monthlyRevenue: 45000 },
  { id: 'g2', name: 'Gold\'s Gym Maadi', nameAr: 'جولدز جيم المعادي', gymCode: 'GG002', status: 'active', city: 'Cairo', planName: 'Business', activeMemberCount: 820, activeTrainerCount: 24, paymentStatus: 'current', createdAt: '2023-04-10', ownerName: 'Tarek Abdel-Aziz', monthlyRevenue: 95000 },
  { id: 'g3', name: 'PowerHouse Gym', nameAr: 'باور هاوس جيم', gymCode: 'PH003', status: 'active', city: 'Alexandria', planName: 'Pro', activeMemberCount: 310, activeTrainerCount: 8, paymentStatus: 'warning', createdAt: '2023-08-22', ownerName: 'Khaled El-Sherif', monthlyRevenue: 32000 },
  { id: 'g4', name: 'Flex Zone', nameAr: 'فليكس زون', gymCode: 'FZ004', status: 'active', city: 'Giza', planName: 'Starter', activeMemberCount: 180, activeTrainerCount: 5, paymentStatus: 'current', createdAt: '2023-10-05', ownerName: 'Mohamed Ali', monthlyRevenue: 18000 },
  { id: 'g5', name: 'Iron Paradise', nameAr: 'آيرون بارادايس', gymCode: 'IP005', status: 'suspended', city: 'Cairo', planName: 'Pro', activeMemberCount: 0, activeTrainerCount: 0, paymentStatus: 'overdue', createdAt: '2023-07-18', ownerName: 'Amr Soliman', monthlyRevenue: 0 },
  { id: 'g6', name: 'FitLife Hub', nameAr: 'فيت لايف هب', gymCode: 'FL006', status: 'active', city: 'Mansoura', planName: 'Starter', activeMemberCount: 120, activeTrainerCount: 3, paymentStatus: 'current', createdAt: '2024-01-12', ownerName: 'Youssef Adel', monthlyRevenue: 12000 },
  { id: 'g7', name: 'Champions Gym', nameAr: 'تشامبيونز جيم', gymCode: 'CG007', status: 'active', city: 'Cairo', planName: 'Business', activeMemberCount: 650, activeTrainerCount: 18, paymentStatus: 'current', createdAt: '2023-03-01', ownerName: 'Hassan Mahmoud', monthlyRevenue: 78000 },
  { id: 'g8', name: 'Elite Fitness', nameAr: 'إيليت فيتنس', gymCode: 'EF008', status: 'active', city: 'Cairo', planName: 'Pro', activeMemberCount: 380, activeTrainerCount: 10, paymentStatus: 'current', createdAt: '2023-09-14', ownerName: 'Rania Sherif', monthlyRevenue: 42000 },
  { id: 'g9', name: 'Titan Gym', nameAr: 'تيتان جيم', gymCode: 'TG009', status: 'active', city: 'Tanta', planName: 'Free', activeMemberCount: 45, activeTrainerCount: 1, paymentStatus: 'none', createdAt: '2024-03-20', ownerName: 'Ibrahim Nabil', monthlyRevenue: 0 },
  { id: 'g10', name: 'Sunrise Fitness', nameAr: 'صن رايز فيتنس', gymCode: 'SF010', status: 'active', city: 'Alexandria', planName: 'Pro', activeMemberCount: 290, activeTrainerCount: 7, paymentStatus: 'current', createdAt: '2023-11-08', ownerName: 'Mona El-Baz', monthlyRevenue: 31000 },
  { id: 'g11', name: 'Body Zone', nameAr: 'بودي زون', gymCode: 'BZ011', status: 'grace_period', city: 'Cairo', planName: 'Starter', activeMemberCount: 90, activeTrainerCount: 2, paymentStatus: 'overdue', createdAt: '2024-02-01', ownerName: 'Fady Samir', monthlyRevenue: 8500 },
  { id: 'g12', name: 'Muscle Factory', nameAr: 'مصل فاكتوري', gymCode: 'MF012', status: 'active', city: 'Aswan', planName: 'Starter', activeMemberCount: 75, activeTrainerCount: 2, paymentStatus: 'current', createdAt: '2024-01-30', ownerName: 'Ali Nasser', monthlyRevenue: 7500 },
  { id: 'g13', name: 'Peak Performance', nameAr: 'بيك بيرفورمانس', gymCode: 'PP013', status: 'active', city: 'Cairo', planName: 'Business', activeMemberCount: 720, activeTrainerCount: 20, paymentStatus: 'current', createdAt: '2023-05-22', ownerName: 'Sherif Adel', monthlyRevenue: 82000 },
  { id: 'g14', name: 'Ladies First Gym', nameAr: 'ليديز فيرست جيم', gymCode: 'LF014', status: 'active', city: 'Cairo', planName: 'Pro', activeMemberCount: 260, activeTrainerCount: 6, paymentStatus: 'current', createdAt: '2023-12-01', ownerName: 'Dina Khalil', monthlyRevenue: 28000 },
  { id: 'g15', name: 'Strong Body Gym', nameAr: 'سترونج بودي جيم', gymCode: 'SB015', status: 'active', city: 'Luxor', planName: 'Starter', activeMemberCount: 95, activeTrainerCount: 3, paymentStatus: 'current', createdAt: '2024-02-14', ownerName: 'Mahmoud Hassan', monthlyRevenue: 9500 },
  { id: 'g16', name: 'Atlas Gym', nameAr: 'أطلس جيم', gymCode: 'AG016', status: 'active', city: 'Cairo', planName: 'Pro', activeMemberCount: 340, activeTrainerCount: 9, paymentStatus: 'warning', createdAt: '2023-08-30', ownerName: 'Karim Tawfik', monthlyRevenue: 36000 },
  { id: 'g17', name: 'Victory Fitness', nameAr: 'فيكتوري فيتنس', gymCode: 'VF017', status: 'active', city: 'Giza', planName: 'Starter', activeMemberCount: 150, activeTrainerCount: 4, paymentStatus: 'current', createdAt: '2024-01-05', ownerName: 'Hossam Abdel-Fattah', monthlyRevenue: 15000 },
  { id: 'g18', name: 'Royal Gym', nameAr: 'رويال جيم', gymCode: 'RG018', status: 'terminated', city: 'Suez', planName: 'Starter', activeMemberCount: 0, activeTrainerCount: 0, paymentStatus: 'none', createdAt: '2023-06-01', ownerName: 'Walid Ramadan', monthlyRevenue: 0 },
  { id: 'g19', name: 'CrossFit Nile', nameAr: 'كروس فيت نايل', gymCode: 'CN019', status: 'active', city: 'Cairo', planName: 'Business', activeMemberCount: 580, activeTrainerCount: 16, paymentStatus: 'current', createdAt: '2023-04-25', ownerName: 'Bassem Youssef', monthlyRevenue: 72000 },
  { id: 'g20', name: 'FitZone Egypt', nameAr: 'فيت زون إيجيبت', gymCode: 'FE020', status: 'active', city: 'Cairo', planName: 'Pro', activeMemberCount: 400, activeTrainerCount: 11, paymentStatus: 'current', createdAt: '2023-07-08', ownerName: 'Yasser El-Masry', monthlyRevenue: 44000 },
  { id: 'g21', name: 'Delta Gym', nameAr: 'دلتا جيم', gymCode: 'DG021', status: 'active', city: 'Zagazig', planName: 'Free', activeMemberCount: 30, activeTrainerCount: 1, paymentStatus: 'none', createdAt: '2024-04-01', ownerName: 'Abdallah Fathi', monthlyRevenue: 0 },
  { id: 'g22', name: 'Urban Fit', nameAr: 'أوربان فيت', gymCode: 'UF022', status: 'active', city: 'Cairo', planName: 'Pro', activeMemberCount: 350, activeTrainerCount: 9, paymentStatus: 'current', createdAt: '2023-10-20', ownerName: 'Tamer Hosny', monthlyRevenue: 38000 },
  { id: 'g23', name: 'Pharaoh Fitness', nameAr: 'فرعون فيتنس', gymCode: 'PF023', status: 'active', city: 'Hurghada', planName: 'Starter', activeMemberCount: 110, activeTrainerCount: 3, paymentStatus: 'current', createdAt: '2024-02-28', ownerName: 'Ayman Shawky', monthlyRevenue: 11000 },
  { id: 'g24', name: 'Spartan Gym', nameAr: 'سبارتن جيم', gymCode: 'SG024', status: 'active', city: 'Cairo', planName: 'Business', activeMemberCount: 690, activeTrainerCount: 19, paymentStatus: 'current', createdAt: '2023-02-14', ownerName: 'Hazem El-Din', monthlyRevenue: 80000 },
  { id: 'g25', name: 'Wellness Hub', nameAr: 'ويلنس هب', gymCode: 'WH025', status: 'active', city: 'Ismailia', planName: 'Starter', activeMemberCount: 85, activeTrainerCount: 2, paymentStatus: 'current', createdAt: '2024-03-10', ownerName: 'Samir Gad', monthlyRevenue: 8000 },
];

// ──── Pending Gyms ────
export const pendingGyms: PendingGym[] = [
  { id: 'pg1', name: 'Apex Fitness Center', ownerName: 'Hany Fawzy', ownerEmail: 'hany@apexfit.com', ownerPhone: '+201112223344', city: 'Cairo', district: 'Heliopolis', requestedPlan: 'Pro', submittedAt: '2026-04-10T09:30:00', notes: 'Referred by Gold\'s Gym owner' },
  { id: 'pg2', name: 'Nile Valley Gym', ownerName: 'Sameh Ragab', ownerEmail: 'sameh@nilegym.com', ownerPhone: '+201556677889', city: 'Assiut', district: 'Downtown', requestedPlan: 'Starter', submittedAt: '2026-04-09T14:20:00' },
  { id: 'pg3', name: 'Desert Storm Fitness', ownerName: 'Wael Abdel-Maguid', ownerEmail: 'wael@dstorm.com', ownerPhone: '+201009988776', city: 'Sharm El-Sheikh', district: 'Nabq Bay', requestedPlan: 'Business', submittedAt: '2026-04-08T11:45:00', notes: 'Tourist area gym, international clientele' },
];

// ──── Subscription Plans ────
export const subscriptionPlans: SubscriptionPlan[] = [
  { id: 'p1', code: 'free', name: 'Free', nameAr: 'مجاني', tier: 'free', monthlyPriceEgp: 0, annualPriceEgp: 0, memberLimit: 50, trainerLimit: 1, staffLimit: 1, description: 'Basic gym management', isActive: true, isPublic: true, sortOrder: 0, createdAt: '2024-01-01' },
  { id: 'p2', code: 'starter', name: 'Starter', nameAr: 'مبتدئ', tier: 'starter', monthlyPriceEgp: 999, annualPriceEgp: 9990, memberLimit: 200, trainerLimit: 5, staffLimit: 3, description: 'For growing gyms', isActive: true, isPublic: true, sortOrder: 1, createdAt: '2024-01-01' },
  { id: 'p3', code: 'pro', name: 'Pro', nameAr: 'احترافي', tier: 'pro', monthlyPriceEgp: 2499, annualPriceEgp: 24990, memberLimit: 500, trainerLimit: 15, staffLimit: 10, description: 'Full-featured gym platform', isActive: true, isPublic: true, sortOrder: 2, createdAt: '2024-01-01' },
  { id: 'p4', code: 'business', name: 'Business', nameAr: 'أعمال', tier: 'business', monthlyPriceEgp: 4999, annualPriceEgp: 49990, memberLimit: 2000, trainerLimit: 50, staffLimit: 25, description: 'Enterprise-grade management', isActive: true, isPublic: true, sortOrder: 3, createdAt: '2024-01-01' },
];

// ──── Features ────
export const features: Feature[] = [
  { id: 'f1', code: 'workout_builder', name: 'Workout Builder', nameAr: 'منشئ التمارين', category: 'Training', isActive: true },
  { id: 'f2', code: 'nutrition_tracking', name: 'Nutrition Tracking', nameAr: 'تتبع التغذية', category: 'Nutrition', isActive: true },
  { id: 'f3', code: 'progress_photos', name: 'Progress Photos', nameAr: 'صور التقدم', category: 'Progress', isActive: true },
  { id: 'f4', code: 'remote_training', name: 'Remote Training', nameAr: 'التدريب عن بعد', category: 'Training', isActive: true },
  { id: 'f5', code: 'custom_exercise_library', name: 'Custom Exercise Library', nameAr: 'مكتبة تمارين مخصصة', category: 'Training', isActive: true },
  { id: 'f6', code: 'session_packages', name: 'Session Packages', nameAr: 'باقات الجلسات', category: 'Billing', isActive: true },
  { id: 'f7', code: 'gym_branding', name: 'Gym Branding', nameAr: 'هوية الجيم', category: 'Settings', isActive: true },
  { id: 'f8', code: 'advanced_analytics', name: 'Advanced Analytics', nameAr: 'تحليلات متقدمة', category: 'Analytics', isActive: true },
  { id: 'f9', code: 'member_leaderboard', name: 'Member Leaderboard', nameAr: 'لوحة متصدري الأعضاء', category: 'Gamification', isActive: true },
  { id: 'f10', code: 'trainer_ratings', name: 'Trainer Ratings', nameAr: 'تقييمات المدربين', category: 'Gamification', isActive: true },
  { id: 'f11', code: 'announcements', name: 'Announcements', nameAr: 'الإعلانات', category: 'Communication', isActive: true },
  { id: 'f12', code: 'multi_staff', name: 'Multi Staff', nameAr: 'تعدد الموظفين', category: 'Identity', isActive: true },
  { id: 'f13', code: 'class_management', name: 'Class Management', nameAr: 'إدارة الحصص', category: 'Scheduling', isActive: true },
  { id: 'f14', code: 'qr_check_in', name: 'QR Check-In', nameAr: 'تسجيل دخول QR', category: 'Gyms', isActive: true },
  { id: 'f15', code: 'meal_plans', name: 'Meal Plans', nameAr: 'خطط الوجبات', category: 'Nutrition', isActive: true },
  { id: 'f16', code: 'body_metrics', name: 'Body Metrics', nameAr: 'قياسات الجسم', category: 'Progress', isActive: true },
  { id: 'f17', code: 'chat', name: 'Chat', nameAr: 'المحادثات', category: 'Communication', isActive: true },
  { id: 'f18', code: 'gamification', name: 'Gamification', nameAr: 'التلعيب', category: 'Gamification', isActive: true },
  { id: 'f19', code: 'testimonials', name: 'Testimonials', nameAr: 'الشهادات', category: 'Gamification', isActive: true },
  { id: 'f20', code: 'seasonal_hours', name: 'Seasonal Hours', nameAr: 'مواعيد موسمية', category: 'Gyms', isActive: true },
  { id: 'f21', code: 'gym_sections', name: 'Gym Sections', nameAr: 'أقسام الجيم', category: 'Gyms', isActive: true },
  { id: 'f22', code: 'export_data', name: 'Export Data', nameAr: 'تصدير البيانات', category: 'Settings', isActive: true },
  { id: 'f23', code: 'bulk_actions', name: 'Bulk Actions', nameAr: 'إجراءات جماعية', category: 'Settings', isActive: true },
  { id: 'f24', code: 'payment_tracking', name: 'Payment Tracking', nameAr: 'تتبع المدفوعات', category: 'Billing', isActive: true },
  { id: 'f25', code: 'streaks', name: 'Streaks', nameAr: 'السلاسل', category: 'Gamification', isActive: true },
  { id: 'f26', code: 'water_tracking', name: 'Water Tracking', nameAr: 'تتبع المياه', category: 'Nutrition', isActive: true },
  { id: 'f27', code: 'live_session_mode', name: 'Live Session Mode', nameAr: 'وضع الجلسة المباشرة', category: 'Training', isActive: true },
  { id: 'f28', code: 'workout_assignment', name: 'Workout Assignment', nameAr: 'تعيين التمارين', category: 'Training', isActive: true },
  { id: 'f29', code: 'trainer_multi_gym', name: 'Trainer Multi-Gym', nameAr: 'مدرب متعدد الأندية', category: 'Training', isActive: true },
  { id: 'f30', code: 'notification_preferences', name: 'Notification Preferences', nameAr: 'إعدادات الإشعارات', category: 'Communication', isActive: true },
  { id: 'f31', code: 'storage_upgrade', name: 'Storage Upgrade', nameAr: 'ترقية التخزين', category: 'Settings', isActive: true },
];

// ──── Feature Matrix (plan x feature) ────
// true = enabled for that plan
export const featureMatrix: Record<string, Record<string, boolean>> = {
  p1: { workout_builder: false, nutrition_tracking: false, progress_photos: false, remote_training: false, custom_exercise_library: false, session_packages: false, gym_branding: false, advanced_analytics: false, member_leaderboard: false, trainer_ratings: false, announcements: false, multi_staff: false, class_management: false, qr_check_in: false, meal_plans: false, body_metrics: true, chat: false, gamification: false, testimonials: false, seasonal_hours: false, gym_sections: false, export_data: false, bulk_actions: false, payment_tracking: true, streaks: false, water_tracking: false, live_session_mode: false, workout_assignment: false, trainer_multi_gym: false, notification_preferences: false, storage_upgrade: false },
  p2: { workout_builder: true, nutrition_tracking: true, progress_photos: true, remote_training: false, custom_exercise_library: false, session_packages: true, gym_branding: false, advanced_analytics: false, member_leaderboard: true, trainer_ratings: true, announcements: true, multi_staff: true, class_management: true, qr_check_in: true, meal_plans: false, body_metrics: true, chat: true, gamification: true, testimonials: false, seasonal_hours: false, gym_sections: true, export_data: true, bulk_actions: false, payment_tracking: true, streaks: true, water_tracking: true, live_session_mode: false, workout_assignment: true, trainer_multi_gym: false, notification_preferences: true, storage_upgrade: false },
  p3: { workout_builder: true, nutrition_tracking: true, progress_photos: true, remote_training: true, custom_exercise_library: false, session_packages: true, gym_branding: true, advanced_analytics: true, member_leaderboard: true, trainer_ratings: true, announcements: true, multi_staff: true, class_management: true, qr_check_in: true, meal_plans: true, body_metrics: true, chat: true, gamification: true, testimonials: true, seasonal_hours: true, gym_sections: true, export_data: true, bulk_actions: true, payment_tracking: true, streaks: true, water_tracking: true, live_session_mode: true, workout_assignment: true, trainer_multi_gym: true, notification_preferences: true, storage_upgrade: false },
  p4: { workout_builder: true, nutrition_tracking: true, progress_photos: true, remote_training: true, custom_exercise_library: true, session_packages: true, gym_branding: true, advanced_analytics: true, member_leaderboard: true, trainer_ratings: true, announcements: true, multi_staff: true, class_management: true, qr_check_in: true, meal_plans: true, body_metrics: true, chat: true, gamification: true, testimonials: true, seasonal_hours: true, gym_sections: true, export_data: true, bulk_actions: true, payment_tracking: true, streaks: true, water_tracking: true, live_session_mode: true, workout_assignment: true, trainer_multi_gym: true, notification_preferences: true, storage_upgrade: true },
};

// ──── Feature Overrides ────
export const featureOverrides: FeatureOverride[] = [
  { id: 'fo1', gymId: 'g1', featureId: 'f4', featureCode: 'remote_training', featureName: 'Remote Training', overrideValue: 'force_on', reason: 'Beta testing partner', createdAt: '2026-03-15', createdBy: 'Ahmed Hassan' },
  { id: 'fo2', gymId: 'g9', featureId: 'f11', featureCode: 'announcements', featureName: 'Announcements', overrideValue: 'force_on', reason: 'Promotional period', createdAt: '2026-04-01', createdBy: 'Sara Mostafa' },
];

// ──── Plan Change Log ────
export const planChangeLogs: PlanChangeLog[] = [
  { id: 'cl1', gymId: 'g4', gymName: 'Flex Zone', previousPlanId: 'p1', previousPlanName: 'Free', newPlanId: 'p2', newPlanName: 'Starter', changedBy: '1', changedByName: 'Ahmed Hassan', reason: 'Gym growth', changedAt: '2026-03-20T10:30:00' },
  { id: 'cl2', gymId: 'g8', gymName: 'Elite Fitness', previousPlanId: 'p2', previousPlanName: 'Starter', newPlanId: 'p3', newPlanName: 'Pro', changedBy: '2', changedByName: 'Sara Mostafa', changedAt: '2026-03-18T14:15:00' },
  { id: 'cl3', gymId: 'g13', gymName: 'Peak Performance', previousPlanId: 'p3', previousPlanName: 'Pro', newPlanId: 'p4', newPlanName: 'Business', changedBy: '1', changedByName: 'Ahmed Hassan', reason: 'Enterprise upgrade', changedAt: '2026-03-10T09:00:00' },
];

// ──── Users (mixed roles for /users page) ────
export const allUsers = [
  { id: 'u1', name: 'Ahmed Hassan', nameAr: 'أحمد حسن', email: 'ahmed@gymapp.com', role: 'platform_admin', adminRole: 'super_admin', status: 'active', createdAt: '2024-01-01', lastLogin: '2026-04-12' },
  { id: 'u2', name: 'Sara Mostafa', nameAr: 'سارة مصطفى', email: 'sara@gymapp.com', role: 'platform_admin', adminRole: 'admin', status: 'active', createdAt: '2024-02-15', lastLogin: '2026-04-12' },
  { id: 'u3', name: 'Mostafa Kamel', nameAr: 'مصطفى كامل', email: 'mostafa@firstgym.com', role: 'gym_owner', status: 'active', createdAt: '2023-06-15', lastLogin: '2026-04-11' },
  { id: 'u4', name: 'Tarek Abdel-Aziz', nameAr: 'طارق عبد العزيز', email: 'tarek@golds.com', role: 'gym_owner', status: 'active', createdAt: '2023-04-10', lastLogin: '2026-04-12' },
  { id: 'u5', name: 'Mariam Sayed', nameAr: 'مريم سيد', email: 'mariam@trainer.com', role: 'trainer', status: 'active', createdAt: '2023-08-01', lastLogin: '2026-04-10' },
  { id: 'u6', name: 'Hassan El-Deeb', nameAr: 'حسن الديب', email: 'hassan@trainee.com', role: 'trainee', status: 'active', createdAt: '2024-01-20', lastLogin: '2026-04-09' },
  { id: 'u7', name: 'Fatma Ali', nameAr: 'فاطمة علي', email: 'fatma@trainee.com', role: 'trainee', status: 'inactive', createdAt: '2023-11-05', lastLogin: '2026-02-15' },
  { id: 'u8', name: 'Youssef Nabil', nameAr: 'يوسف نبيل', email: 'youssef@staff.com', role: 'gym_staff', status: 'active', createdAt: '2024-03-01', lastLogin: '2026-04-12' },
  { id: 'u9', name: 'Laila Ibrahim', nameAr: 'ليلى إبراهيم', email: 'laila@trainer.com', role: 'trainer', status: 'active', createdAt: '2023-09-15', lastLogin: '2026-04-11' },
  { id: 'u10', name: 'Omar Farid', nameAr: 'عمر فريد', email: 'omar@gymapp.com', role: 'platform_admin', adminRole: 'support', status: 'active', createdAt: '2024-03-10', lastLogin: '2026-04-12' },
  { id: 'u11', name: 'Nour Ibrahim', nameAr: 'نور إبراهيم', email: 'nour@gymapp.com', role: 'platform_admin', adminRole: 'analyst', status: 'active', createdAt: '2024-04-01', lastLogin: '2026-04-11' },
  { id: 'u12', name: 'Kareem Tawfik', nameAr: 'كريم توفيق', email: 'kareem@gym.com', role: 'gym_owner', status: 'active', createdAt: '2023-08-30', lastLogin: '2026-04-10' },
  { id: 'u13', name: 'Dina Khalil', nameAr: 'دينا خليل', email: 'dina@ladiesfirst.com', role: 'gym_owner', status: 'active', createdAt: '2023-12-01', lastLogin: '2026-04-11' },
  { id: 'u14', name: 'Amr Soliman', nameAr: 'عمرو سليمان', email: 'amr@ironparadise.com', role: 'gym_owner', status: 'suspended', createdAt: '2023-07-18', lastLogin: '2026-03-01' },
  { id: 'u15', name: 'Rana Mohamed', nameAr: 'رنا محمد', email: 'rana@trainee.com', role: 'trainee', status: 'active', createdAt: '2024-02-10', lastLogin: '2026-04-12' },
  { id: 'u16', name: 'Mahmoud Hassan', nameAr: 'محمود حسن', email: 'mahmoud@trainer.com', role: 'trainer', status: 'active', createdAt: '2023-07-01', lastLogin: '2026-04-09' },
  { id: 'u17', name: 'Nada El-Sayed', nameAr: 'ندى السيد', email: 'nada@trainee.com', role: 'trainee', status: 'active', createdAt: '2024-04-01', lastLogin: '2026-04-12' },
  { id: 'u18', name: 'Hossam Abdel-Fattah', nameAr: 'حسام عبد الفتاح', email: 'hossam@victory.com', role: 'gym_owner', status: 'active', createdAt: '2024-01-05', lastLogin: '2026-04-10' },
  { id: 'u19', name: 'Salma Adel', nameAr: 'سلمى عادل', email: 'salma@staff.com', role: 'gym_staff', status: 'active', createdAt: '2024-03-15', lastLogin: '2026-04-11' },
  { id: 'u20', name: 'Bassem Youssef', nameAr: 'باسم يوسف', email: 'bassem@crossfitnile.com', role: 'gym_owner', status: 'active', createdAt: '2023-04-25', lastLogin: '2026-04-12' },
  { id: 'u21', name: 'Yasmin Khaled', nameAr: 'ياسمين خالد', email: 'yasmin@trainee.com', role: 'trainee', status: 'active', createdAt: '2024-01-30', lastLogin: '2026-04-08' },
  { id: 'u22', name: 'Tamer Hosny', nameAr: 'تامر حسني', email: 'tamer@urbanfit.com', role: 'gym_owner', status: 'active', createdAt: '2023-10-20', lastLogin: '2026-04-12' },
  { id: 'u23', name: 'Aisha Mohamed', nameAr: 'عائشة محمد', email: 'aisha@trainer.com', role: 'trainer', status: 'pending', createdAt: '2026-04-10', lastLogin: undefined },
  { id: 'u24', name: 'Hazem El-Din', nameAr: 'حازم الدين', email: 'hazem@spartan.com', role: 'gym_owner', status: 'active', createdAt: '2023-02-14', lastLogin: '2026-04-11' },
  { id: 'u25', name: 'Mona El-Baz', nameAr: 'منى الباز', email: 'mona@sunrisefit.com', role: 'gym_owner', status: 'active', createdAt: '2023-11-08', lastLogin: '2026-04-10' },
  { id: 'u26', name: 'Fady Samir', nameAr: 'فادي سمير', email: 'fady@bodyzone.com', role: 'gym_owner', status: 'active', createdAt: '2024-02-01', lastLogin: '2026-04-09' },
  { id: 'u27', name: 'Reem Abdallah', nameAr: 'ريم عبدالله', email: 'reem@trainee.com', role: 'trainee', status: 'active', createdAt: '2024-03-20', lastLogin: '2026-04-12' },
  { id: 'u28', name: 'Khaled El-Sherif', nameAr: 'خالد الشريف', email: 'khaled@powerhouse.com', role: 'gym_owner', status: 'active', createdAt: '2023-08-22', lastLogin: '2026-04-11' },
  { id: 'u29', name: 'Noha Ahmed', nameAr: 'نهى أحمد', email: 'noha@staff.com', role: 'gym_staff', status: 'active', createdAt: '2024-02-20', lastLogin: '2026-04-10' },
  { id: 'u30', name: 'Ali Nasser', nameAr: 'علي ناصر', email: 'ali@musclefactory.com', role: 'gym_owner', status: 'active', createdAt: '2024-01-30', lastLogin: '2026-04-08' },
];

// ──── At-Risk Gyms ────
export const atRiskGyms: AtRiskGym[] = [
  { id: 'g11', name: 'Body Zone', planName: 'Starter', riskScore: 85, riskFactors: ['Payment overdue', 'Declining members', 'Low engagement'], lastActivity: '2026-04-05', memberCount: 90, monthlyRevenue: 8500 },
  { id: 'g3', name: 'PowerHouse Gym', planName: 'Pro', riskScore: 62, riskFactors: ['Payment warning', 'Trainer turnover'], lastActivity: '2026-04-10', memberCount: 310, monthlyRevenue: 32000 },
  { id: 'g16', name: 'Atlas Gym', planName: 'Pro', riskScore: 55, riskFactors: ['Payment warning', 'Decreasing check-ins'], lastActivity: '2026-04-11', memberCount: 340, monthlyRevenue: 36000 },
];

// ──── Analytics Data ────
export const planDistribution: PlanDistribution[] = [
  { plan: 'Free', count: 2, percentage: 8, color: '#94A3B8' },
  { plan: 'Starter', count: 8, percentage: 32, color: '#3B82F6' },
  { plan: 'Pro', count: 9, percentage: 36, color: '#8B5CF6' },
  { plan: 'Business', count: 6, percentage: 24, color: '#F59E0B' },
];

export const revenueTrend: TimeSeriesPoint[] = [
  { date: '2025-11', value: 245000 },
  { date: '2025-12', value: 258000 },
  { date: '2026-01', value: 262000 },
  { date: '2026-02', value: 270000 },
  { date: '2026-03', value: 278000 },
  { date: '2026-04', value: 284000 },
];

export const gymGrowth: TimeSeriesPoint[] = [
  { date: '2025-11', value: 118 },
  { date: '2025-12', value: 124 },
  { date: '2026-01', value: 128 },
  { date: '2026-02', value: 133 },
  { date: '2026-03', value: 138 },
  { date: '2026-04', value: 142 },
];

export const cityRevenue: CityRevenue[] = [
  { city: 'Cairo', revenue: 542000, gymCount: 12 },
  { city: 'Alexandria', revenue: 63000, gymCount: 2 },
  { city: 'Giza', revenue: 33000, gymCount: 2 },
  { city: 'Mansoura', revenue: 12000, gymCount: 1 },
  { city: 'Tanta', revenue: 0, gymCount: 1 },
  { city: 'Aswan', revenue: 7500, gymCount: 1 },
  { city: 'Luxor', revenue: 9500, gymCount: 1 },
  { city: 'Hurghada', revenue: 11000, gymCount: 1 },
];

export const featureUsageData: FeatureUsage[] = [
  { featureCode: 'workout_builder', featureName: 'Workout Builder', usageCount: 4250, adoptionRate: 78 },
  { featureCode: 'qr_check_in', featureName: 'QR Check-In', usageCount: 12400, adoptionRate: 92 },
  { featureCode: 'nutrition_tracking', featureName: 'Nutrition Tracking', usageCount: 2100, adoptionRate: 45 },
  { featureCode: 'class_management', featureName: 'Class Management', usageCount: 3800, adoptionRate: 71 },
  { featureCode: 'announcements', featureName: 'Announcements', usageCount: 890, adoptionRate: 56 },
  { featureCode: 'session_packages', featureName: 'Session Packages', usageCount: 1560, adoptionRate: 64 },
  { featureCode: 'advanced_analytics', featureName: 'Advanced Analytics', usageCount: 780, adoptionRate: 52 },
  { featureCode: 'remote_training', featureName: 'Remote Training', usageCount: 320, adoptionRate: 21 },
];

// ──── Support Tickets ────
export interface SupportTicketMessage {
  sender: string;
  text: string;
  time: string;
  isInternal?: boolean;
}

export interface SupportTicket {
  id: string;
  number: string;
  subject: string;
  gym: string;
  priority: 'high' | 'medium' | 'low';
  status: 'open' | 'in_progress' | 'resolved' | 'closed';
  assignedTo: string;
  created: string;
  messages: SupportTicketMessage[];
}

export const supportTickets: SupportTicket[] = [
  {
    id: 't1', number: 'TKT-001', subject: 'Unable to add new trainer', gym: 'First Gym',
    priority: 'high', status: 'open', assignedTo: 'Omar Farid', created: '2026-04-10',
    messages: [
      { sender: 'Mostafa Kamel', text: 'I am trying to add a new trainer but the button is greyed out.', time: '2026-04-10 09:15' },
      { sender: 'Omar Farid', text: 'Could you check your plan trainer limit? You may have reached the max.', time: '2026-04-10 09:45', isInternal: false },
    ],
  },
  {
    id: 't2', number: 'TKT-002', subject: 'Billing invoice discrepancy', gym: "Gold's Gym Maadi",
    priority: 'high', status: 'in_progress', assignedTo: 'Omar Farid', created: '2026-04-09',
    messages: [
      { sender: 'Tarek Abdel-Aziz', text: 'The invoice for March shows a different amount than expected.', time: '2026-04-09 14:20' },
      { sender: 'Omar Farid', text: 'Investigating now, will update shortly.', time: '2026-04-09 15:00' },
      { sender: 'Omar Farid', text: 'This was a proration issue. Fix deployed.', time: '2026-04-11 11:00', isInternal: true },
    ],
  },
  {
    id: 't3', number: 'TKT-003', subject: 'QR scanner not working on kiosk', gym: 'PowerHouse Gym',
    priority: 'medium', status: 'open', assignedTo: 'Support Team', created: '2026-04-11',
    messages: [
      { sender: 'Khaled El-Sherif', text: 'Our QR scanner stopped working after the latest app update.', time: '2026-04-11 08:30' },
    ],
  },
  {
    id: 't4', number: 'TKT-004', subject: 'Export members to CSV', gym: 'Champions Gym',
    priority: 'low', status: 'resolved', assignedTo: 'Support Team', created: '2026-04-08',
    messages: [
      { sender: 'Hassan Mahmoud', text: 'How can I export my member list?', time: '2026-04-08 10:00' },
      { sender: 'Support Team', text: 'Go to Members → Export → CSV. Done!', time: '2026-04-08 10:30' },
    ],
  },
  {
    id: 't5', number: 'TKT-005', subject: 'Announcement not showing in app', gym: 'Elite Fitness',
    priority: 'medium', status: 'open', assignedTo: 'Support Team', created: '2026-04-12',
    messages: [
      { sender: 'Rania Sherif', text: 'I posted an announcement but members say they cannot see it.', time: '2026-04-12 07:00' },
    ],
  },
  {
    id: 't6', number: 'TKT-006', subject: 'Session package deduction incorrect', gym: 'Atlas Gym',
    priority: 'high', status: 'in_progress', assignedTo: 'Omar Farid', created: '2026-04-07',
    messages: [
      { sender: 'Karim Tawfik', text: 'When a session is cancelled late, it is not deducting from the package.', time: '2026-04-07 16:00' },
      { sender: 'Omar Farid', text: 'Checking the gym session policy configuration.', time: '2026-04-07 16:45' },
    ],
  },
];
