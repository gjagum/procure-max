export interface Paged<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface PageQuery {
  page?: number;
  pageSize?: number;
  search?: string;
}

export interface IdResponse {
  id: string;
}

export interface UserProfile {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
  permissions: string[];
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  userProfile: UserProfile;
}

export interface SupplierSummary {
  id: string;
  code: string;
  name: string;
  legalName?: string;
  isActive: boolean;
  isBlocked: boolean;
  currency: string;
}

export interface ItemSummary {
  id: string;
  sku?: string;
  name: string;
  category?: string;
  defaultPriceMinor: number;
  defaultCurrency: string;
  isActive: boolean;
}

export interface UserSummary {
  id: string;
  email: string;
  fullName: string;
  isActive: boolean;
  roles: string[];
  createdAt: string;
  rowVersion: string;
}

export type RequisitionStatus =
  | "DRAFT"
  | "PENDING_APPROVAL"
  | "APPROVED"
  | "REJECTED";

export interface RequisitionSummary {
  id: string;
  number: string;
  requestor: string;
  costCenter?: string;
  totalMinor: number;
  currency: string;
  status: RequisitionStatus;
  createdAt: string;
}
