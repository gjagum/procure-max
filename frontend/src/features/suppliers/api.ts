import { http, buildQueryString } from "@/lib/http";
import type { Paged, SupplierSummary, PageQuery } from "@/lib/types";

export async function fetchSuppliers(
  params: PageQuery,
): Promise<Paged<SupplierSummary>> {
  return http<Paged<SupplierSummary>>(
    `/suppliers${buildQueryString(params)}`,
  );
}
