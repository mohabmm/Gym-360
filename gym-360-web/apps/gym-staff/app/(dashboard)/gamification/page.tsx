'use client';

import { useState } from 'react';
import { PageHeader, ConfirmModal } from '@gym360/ui';
import { useMemberLeaderboard, useTrainerRankings, useTestimonials, useModerateTestimonial } from '@/lib/hooks/useGamification';
import { UnderlineTabs } from '@/components/UnderlineTabs';
import { MemberLeaderboardTab, CoachRankingsTab, TestimonialsTab } from '@/components/gamification/GamificationTabs';

const tabs = ['Member Leaderboard', 'Coach Rankings', 'Testimonials'] as const;

export default function GamificationPage() {
  const { data: leaderboard = [] } = useMemberLeaderboard();
  const { data: trainerRankings = [] } = useTrainerRankings();
  const { data: testimonials = [] } = useTestimonials();
  const moderateTestimonial = useModerateTestimonial();

  const [activeTab, setActiveTab] = useState<(typeof tabs)[number]>('Member Leaderboard');
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

      <UnderlineTabs tabs={tabs} active={activeTab} onChange={setActiveTab} />

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
