export interface TeamMember {
  id: string;
  name: string;
  isLead: boolean;
  isActive: boolean;
  createdAt: string;
}
export interface CreateTeamMemberDto { name: string; }
export interface UpdateTeamMemberDto { name: string; }
