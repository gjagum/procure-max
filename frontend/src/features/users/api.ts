import { http, buildQueryString } from "@/lib/http";
import type { Paged, UserSummary, PageQuery } from "@/lib/types";

export async function fetchUsers(
  params: PageQuery,
): Promise<Paged<UserSummary>> {
  return http<Paged<UserSummary>>(`/users${buildQueryString(params)}`);
}
