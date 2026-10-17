interface Props<T extends string> {
  tabs: readonly T[];
  active: T;
  onChange: (tab: T) => void;
}

/** Text tabs with an underline on the active one (members, gamification). */
export function UnderlineTabs<T extends string>({ tabs, active, onChange }: Props<T>) {
  return (
    <div className="border-b">
      <div className="flex gap-6">
        {tabs.map((tab) => (
          <button key={tab} onClick={() => onChange(tab)} className={`pb-3 text-sm font-medium border-b-2 ${active === tab ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground'}`}>
            {tab}
          </button>
        ))}
      </div>
    </div>
  );
}
