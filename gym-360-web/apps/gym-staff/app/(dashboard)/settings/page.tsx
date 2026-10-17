'use client';

import { useEffect, useState } from 'react';
import { PageHeader } from '@gym360/ui';
import { toast } from 'sonner';
import { useGym, useUpdateGym, useUpdateBranding, type GymProfile } from '@/lib/hooks/useGym';
import { apiErrorMessage } from '@/lib/api-error';
import { BrandingSection } from '@/components/settings/BrandingSection';
import { EMPTY_PROFILE, GymProfileSection, type GymProfileForm } from '@/components/settings/GymProfileSection';
import {
  AmenitiesSection,
  NotificationsSection,
  OperatingHoursSection,
  SocialMediaSection,
} from '@/components/settings/PreferenceSections';

// Server fields of the profile form. Alt phones are not stored, so they keep
// whatever the user typed.
function profileFromGym(gym: GymProfile) {
  return {
    name: gym.name ?? '',
    description: gym.description ?? '',
    phone: gym.phone ?? '',
    email: gym.email ?? '',
    website: gym.website ?? '',
    street: gym.street ?? '',
    city: gym.city ?? '',
    area: gym.area ?? '',
  };
}

export default function SettingsPage() {
  const { data: gym } = useGym();
  const updateGym = useUpdateGym();
  const updateBranding = useUpdateBranding();
  const [profile, setProfile] = useState<GymProfileForm>(EMPTY_PROFILE);
  const [primaryColor, setPrimaryColor] = useState('#C41E3A');
  const [accentColor, setAccentColor] = useState('#D4AF37');
  // Only push branding when the user actually edits a color — branding is a
  // Pro-gated endpoint (400 otherwise), so an untouched save must not call it
  // and sink an otherwise-fine profile save.
  const [colorsTouched, setColorsTouched] = useState(false);

  useEffect(() => {
    if (!gym) return;
    setProfile((current) => ({ ...current, ...profileFromGym(gym) }));
    if (gym.primaryColor) setPrimaryColor(gym.primaryColor);
    if (gym.secondaryColor) setAccentColor(gym.secondaryColor);
  }, [gym]);

  const updateField = (field: keyof GymProfileForm, value: string) =>
    setProfile((current) => ({ ...current, [field]: value }));

  // Branding only when the user actually changed a color. Dedicated endpoint;
  // the BE replaces the whole Branding object, so pass through existing
  // logo/cover to avoid wiping them. Returns false when the save failed.
  const saveBrandingIfTouched = async () => {
    if (!colorsTouched) return true;
    try {
      await updateBranding.mutateAsync({
        primaryColor,
        secondaryColor: accentColor,
        logoUrl: gym?.logoUrl,
        coverPhotoUrl: gym?.coverPhotoUrl,
      });
      setColorsTouched(false);
      return true;
    } catch (err: unknown) {
      toast.error(apiErrorMessage(err, 'Profile saved, but branding colors need a Pro plan.'));
      return false;
    }
  };

  const handleSave = async () => {
    // Profile save first — independent of branding so a Pro-gated branding
    // failure can't mask a successful profile save.
    try {
      const { altPhones: _altPhones, ...fields } = profile;
      await updateGym.mutateAsync(fields);
    } catch (err: unknown) {
      toast.error(apiErrorMessage(err, 'Failed to save settings.'));
      return;
    }

    if (await saveBrandingIfTouched()) {
      toast.success('Settings saved successfully!');
    }
  };

  const onColorChange = (setColor: (color: string) => void) => (color: string) => {
    setColor(color);
    setColorsTouched(true);
  };

  return (
    <div className="space-y-6">
      <PageHeader title="Settings" description="Manage your gym profile and preferences" />
      <GymProfileSection profile={profile} onChange={updateField} onSave={handleSave} />
      <OperatingHoursSection />
      <BrandingSection
        gymName={profile.name}
        primaryColor={primaryColor}
        accentColor={accentColor}
        onPrimaryColorChange={onColorChange(setPrimaryColor)}
        onAccentColorChange={onColorChange(setAccentColor)}
      />
      <AmenitiesSection />
      <SocialMediaSection />
      <NotificationsSection />
    </div>
  );
}
