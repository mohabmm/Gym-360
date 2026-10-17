import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/settings_provider.dart';
import '../../../../core/error_text.dart';

class AmenitiesScreen extends ConsumerStatefulWidget {
  const AmenitiesScreen({super.key});

  @override
  ConsumerState<AmenitiesScreen> createState() => _AmenitiesScreenState();
}

class _AmenitiesScreenState extends ConsumerState<AmenitiesScreen> {
  final _addController = TextEditingController();

  @override
  void dispose() {
    _addController.dispose();
    super.dispose();
  }

  Future<void> _addAmenity() async {
    final value = _addController.text.trim();
    if (value.isEmpty) return;
    try {
      final gymId = ref.read(currentGymIdProvider);
      final apiClient = ref.read(apiClientProvider);
      final gym = await apiClient.get<Map<String, dynamic>>(
        ApiEndpoints.gymById(GymId(gymId)),
        parser: (data) => data as Map<String, dynamic>,
      );
      final current =
          ((gym['amenities'] as List?) ?? []).map((e) => e.toString()).toList();
      if (current.contains(value)) return;
      await apiClient.put<void>(
        ApiEndpoints.gymById(GymId(gymId)),
        data: _buildUpdatePayload(gym, [...current, value]),
        parser: (_) {},
      );
      ref.invalidate(gymAmenitiesProvider);
      _addController.clear();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(staffErrorText(e))));
      }
    }
  }

  Map<String, dynamic> _buildUpdatePayload(
      Map<String, dynamic> gym, List<String> amenities) {
    final address = gym['address'] as Map<String, dynamic>? ?? {};
    final contact = gym['contact'] as Map<String, dynamic>? ?? {};
    return {
      'name': gym['name'],
      'description': gym['description'],
      'address': {
        'street': address['street'],
        'city': address['city'],
        'state': address['state'],
        'country': address['country'],
        'zipCode': address['zipCode'],
      },
      'contact': {
        'email': contact['email'],
        'phone': contact['phone'],
        'website': contact['website'],
      },
      'amenities': amenities,
    };
  }

  @override
  Widget build(BuildContext context) {
    final amenities = ref.watch(gymAmenitiesProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'مرافق الصالة' : 'Gym Amenities'),
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                Expanded(
                  child: AppTextField(
                    controller: _addController,
                    hint: isArabic ? 'مثال: مسبح، ساونا...' : 'e.g. Pool, Sauna...',
                  ),
                ),
                const SizedBox(width: 8),
                IconButton(
                  icon: const Icon(Icons.add_circle),
                  onPressed: _addAmenity,
                  color: Theme.of(context).colorScheme.primary,
                ),
              ],
            ),
          ),
          Expanded(
            child: amenities.when(
              data: (items) {
                if (items.isEmpty) {
                  return EmptyState(
                    icon: Icons.hotel_outlined,
                    title: isArabic ? 'لا توجد مرافق' : 'No amenities',
                  );
                }
                return ListView.builder(
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  itemCount: items.length,
                  itemBuilder: (context, index) {
                    final amenity = items[index];
                    return AppCard(
                      margin: const EdgeInsets.only(bottom: 8),
                      child: ListTile(
                        contentPadding: EdgeInsets.zero,
                        leading: const Icon(Icons.check_circle_outline, color: AppColors.success),
                        title: Text(amenity),
                        trailing: IconButton(
                          icon: const Icon(Icons.delete_outline, color: AppColors.error),
                          onPressed: () async {
                            try {
                              final gymId = ref.read(currentGymIdProvider);
                              final apiClient = ref.read(apiClientProvider);
                              final gym = await apiClient.get<Map<String, dynamic>>(
                                ApiEndpoints.gymById(GymId(gymId)),
                                parser: (data) => data as Map<String, dynamic>,
                              );
                              final current = ((gym['amenities'] as List?) ?? [])
                                  .map((e) => e.toString())
                                  .toList();
                              await apiClient.put<void>(
                                ApiEndpoints.gymById(GymId(gymId)),
                                data: _buildUpdatePayload(
                                  gym,
                                  current.where((a) => a != amenity).toList(),
                                ),
                                parser: (_) {},
                              );
                              ref.invalidate(gymAmenitiesProvider);
                            } catch (e) {
                              // Was silently swallowed; surface the failure so
                              // the user knows the delete didn't take (parity
                              // with the add-amenity flow).
                              if (context.mounted) {
                                ScaffoldMessenger.of(context).showSnackBar(
                                  SnackBar(
                                    content: Text(isArabic
                                        ? 'تعذر حذف المرفق. حاول مرة اخرى.'
                                        : 'Could not remove the amenity. Please try again.'),
                                  ),
                                );
                              }
                            }
                          },
                        ),
                      ),
                    );
                  },
                );
              },
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (e, _) => AppErrorWidget(
                message: isArabic ? 'خطأ في التحميل' : 'Error loading amenities',
                onRetry: () => ref.invalidate(gymAmenitiesProvider),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
