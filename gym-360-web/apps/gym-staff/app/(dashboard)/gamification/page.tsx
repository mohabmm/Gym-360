'use client';

import { useState } from 'react';
import { PageHeader, ConfirmModal, FEATURE_FLAG_KEYS as K } from '@gym360/ui';
import { useStaffFlags } from '@/lib/feature-flags';
import { useMemberLeaderboard, useTrainerRankings, useTestimonials, useModerateTestimonial } from '@/lib/hooks/useGamification';
import { UnderlineTabs } from '@/components/UnderlineTabs';
import { MemberLeaderboardTab, CoachRankingsTab, TestimonialsTab } from '@/components/gamification/GamificationTabs';

const ALL_TABS = ['Member Leaderboard', 'Coach Rankings', 'Testimonials'] as const;
type Tab = (typeof ALL_TABS)[number];

/** Each tab is its own flag; the page itself shows when any of them is ON. */
const TAB_FLAG: Record<Tab, string> = {
  'Member Leaderboard': K.GAMIFICATION_MEMBER_LEADERBOARD,
  'Coach Rankings': K.COACH_LEADERBOARD,
  Testimonials: K.COACH_TESTIMONIALS,
};

export default function GamificationPage() {
  const { isEnabled } = useStaffFlags();
  const tabs = ALL_TABS.filter((tab) => isEnabled(TAB_FLAG[tab]));
  const { data: leaderboard = [] } = useMemberLeaderboard(tabs.includes('Member Leaderboard'));
  const { data: trainerRankings = [] } = useTrainerRankings(tabs.includes('Coach Rankings'));
  const { data: testimonials = [] } = useTestimonials(tabs.includes('Testimonials'));
  const moderateTestimonial = useModerateTestimonial();

  const [selectedTab, setActiveTab] = useState<Tab>('Member Leaderboard');
  const activeTab: Tab | undefined = tabs.includes(selectedTab) ? selectedTab : tabs[0];
  const [rejectTarget, setRejectTarget] = useState<string | null>(null);

  const handleApproveTestimonial = (id: string) => {
    moderateTestimonial.mutate({ id, status: 'approved' });
  };
  const handleRejectTestimonial = (id: string) => {
    setRejectTarget(id);
  };
  const confirmRejectTestimonial = () => {
    if (!rejectTarget) return;
    moderateTestimonial.mutate({ id: rejectTarget, status: 'rejected' });
    setRejectTarget(null);
  };

  return (
    <div className="space-y-6">
      <PageHeader title="Gamification" description="Leaderboards, rankings, and testimonials" />

      {activeTab && <UnderlineTabs tabs={tabs} active={activeTab} onChange={setActiveTab} />}

      {activeTab === 'Member Leaderboard' && <MemberLeaderboardTab leaderboard={leaderboard} />}

      {activeTab === 'Coach Rankings' && <CoachRankingsTab rankings={trainerRankings} />}

      {activeTab === 'Testimonials' && (
        <TestimonialsTab
          testimonials={testimonials}
          onApprove={handleApproveTestimonial}
          onReject={handleRejectTestimonial}
          moderating={moderateTestimonial.isPending}
        />
      )}

      <ConfirmModal
        open={!!rejectTarget}
        onClose={() => setRejectTarget(null)}
        onConfirm={confirmRejectTestimonial}
        title="Reject Testimonial"
        description="Reject this testimonial? It will be removed from the public feed."
        confirmLabel="Reject"
        variant="danger"
      />
    </div>
  );
}
